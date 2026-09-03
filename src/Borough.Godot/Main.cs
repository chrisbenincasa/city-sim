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

    /// <summary>How wide the carriageway is drawn. A drawing width, and no Segment states one.</summary>
    private const float RoadWidthMetres = 8f;

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
        (RoadWidthMetres * 0.5f) + (BlockPatterns.StripTiles(block, lots) * MetresPerTile) + 1f;

    /// <summary>Above this, a Building gets a flat roof. <b>Tall things are not gabled.</b></summary>
    private const float PitchCeilingMetres = 24f;


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
    /// more than Courtyard</em> is not. A monotone ramp asserts the second. ⚠ <b>Five, and the
    /// count is <c>BlockPatterns.Count</c>'s</b>; a sixth pattern needs a sixth colour here and
    /// <see cref="RungOf"/> clamps rather than crashing until it gets one.
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
    private MultiMeshInstance3D _yards = null!;
    private MultiMeshInstance3D _trees = null!;
    private MultiMeshInstance3D _rocks = null!;
    private MultiMeshInstance3D _travellers = null!;
    private MultiMeshInstance3D _roads = null!;
    private MultiMeshInstance3D _plots = null!;
    private PanelContainer _tuner = null!;

    private LineEdit[] _fields = [];

    private Label _tunerStatus = null!;

    /// <summary>The Ruleset's TEXT, kept because the tuner rewrites text and re-parses it.</summary>
    private string _toml = string.Empty;

    private int _citizens;

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
    private StandardMaterial3D _skinned = null!;

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

    private readonly List<ulong> _yardIds = [];

    /// <summary>Which Citizen each drawn Traveller is, in instance order.</summary>
    private readonly List<ulong> _travellerIds = [];

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
        (_rulesetPath, int citizens, ulong startAt, bool govern, string? drive, ulong quitAt,
            string? listen, string? record) = Arguments();

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
        _log.Append(Ticks.Zero, new Command(CommandKind.Populate, default, default));
        _simulation.Step(new TickInput([new Command(CommandKind.Populate, default, default)], 0));

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
        _roads = Layer(new Color(0.30f, 0.30f, 0.33f), new Vector3(1f, 0.1f, 1f), casts: false);

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

        // A SECOND MESH AND NOT A SECOND BOX. A roof is the only part of a Building that is not a
        // cuboid, and a silhouette is what reads at the camera this game is played at -- so the
        // gable is a PrismMesh off the shelf (adr/0018) rather than geometry anybody wrote.
        _roofs = Layer(Roofing, new PrismMesh { Size = Vector3.One }, perInstance: true);

        // THE COURTYARD. A block's middle cannot hold a Lot (adr/0078) and these are not Lots: an
        // outbuilding belongs to the Building in front of it and is drawn from its scramble, which
        // is DepthFillLow's own bargain -- geometry the city does not have, invented so the
        // picture reads as a city, and labelled so nobody promotes it to a Ruleset key.
        _yards = Layer(Outbuilding, Vector3.One, perInstance: true);
        _trees = Layer(Crown, new SphereMesh { Radius = 0.5f, Height = 1f, RadialSegments = 6, Rings = 3 });
        _rocks = Layer(Boulder, Vector3.One);
        _travellers = Layer(new Color(1.0f, 0.45f, 0.15f), new Vector3(3f, 3f, 3f));

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

    /// <summary>Apply every command the world has reached, in file order.</summary>
    /// <remarks>
    /// ⚠ <b>Nothing runs before the third frame, and that is not pedantry.</b> A <c>Control</c>
    /// added to a <c>CanvasLayer</c> has not been laid out in the frame it was added, so the
    /// readout is absent from a picture taken too early -- and with <c>--start-at</c> the world is
    /// already past the first command when the first frame draws. ***The one thing in a frame that
    /// says which Tick it is, is the thing an early capture drops.*** Two photographs of a flood
    /// were taken with no caption before anybody noticed the panel was missing rather than empty.
    /// <b>Nothing is lost by waiting</b>: the clock is held at the command's Tick until it runs.
    /// </remarks>
    private void Drive()
    {
        if (_frame++ < 2)
        {
            return;
        }

        while (_next < _drive.Length && _world.Tick.Raw >= _drive[_next].At)
        {
            Apply(_drive[_next++]);
        }
    }

    /// <summary>
    /// Read the drive script, and hang <c>--quit-at</c> off the end of it as one more command.
    /// </summary>
    /// <remarks>
    /// ⚠ <b><c>--quit-at</c> is a command and not a second mechanism</b>, which answers
    /// <c>plans/0048</c> <b>D3</b>: <c>--drive</c> does NOT imply an end, because a script a person
    /// is watching should keep running when it runs out of instructions. Wanting an end means
    /// writing <c>quit</c> or passing the flag, and both arrive at the same applier.
    /// </remarks>
    private bool Driven(string? script, ulong quitAt)
    {
        List<DriveCommand> commands = [];

        if (script is not null)
        {
            string path = Globalize(script);

            if (!File.Exists(path))
            {
                GD.PrintErr($"no drive script at {path}.");

                return false;
            }

            DriveScriptResult read = DriveScript.Parse(
                File.ReadAllText(path), Path.GetFileName(path));

            if (read.Commands is null)
            {
                GD.PrintErr(read.Describe());

                return false;
            }

            commands.AddRange(read.Commands);
        }

        if (quitAt > 0)
        {
            if (commands.Count > 0 && commands[^1].At > quitAt)
            {
                GD.PrintErr($"--quit-at {quitAt:N0} is before the script's last command at Tick "
                    + $"{commands[^1].At:N0}, so that command could never run.");

                return false;
            }

            if (DriveScript.Stopped(commands) && commands.Count > 0 && quitAt > commands[^1].At)
            {
                GD.PrintErr($"the script stops the clock at Tick {commands[^1].At:N0}, so "
                    + $"--quit-at {quitAt:N0} is never reached. Resume before the end.");

                return false;
            }

            commands.Add(new DriveCommand(quitAt, DriveVerb.Quit, 0, null));
        }

        _drive = [.. commands];

        return true;
    }

    /// <summary>
    /// <b>The one control surface.</b> The keyboard and the script both arrive here.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>Every verb is an ABSOLUTE and the keyboard is what toggles.</b> <c>g</c> reads the
    /// carriageway's current visibility and asks for the opposite; a script says <c>roads off</c>
    /// and means it whatever ran before. Two appliers would drift, and the one that drifted would
    /// be the one nobody presses.
    /// </remarks>
    private void Apply(DriveCommand command)
    {
        switch (command.Verb)
        {
            case DriveVerb.Pause:
                _rung = 0;

                break;

            case DriveVerb.Resume:
                _rung = _resume;

                break;

            case DriveVerb.Speed:
                // Clamped rather than refused: the ladder's length is the shell's own fact and the
                // format cannot know it, so the parser checks the shape and this checks the range.
                _rung = Math.Clamp(command.Amount, 0, Ladder.Length - 1);

                break;

            case DriveVerb.Roads:
                _roads.Visible = command.Amount != 0;

                break;

            case DriveVerb.Cells:
                _cells.Visible = command.Amount != 0;

                break;

            case DriveVerb.Turn:
                // Modulo rather than accumulation: a session is long and a yaw is an angle.
                _yaw = (_yaw + (command.Amount * YawStepRadians)) % (Mathf.Pi * 2f);

                Orbit();

                break;

            case DriveVerb.Zoom:
                Dolly(command.Amount);

                break;

            case DriveVerb.Shoot:
                Shoot(command.Path!);

                break;

            case DriveVerb.Readout:
                Caption(command.Path!);

                break;

            case DriveVerb.Draw:
                DrawList(command.Path!);

                break;

            case DriveVerb.Hold:
                Hold(command.Path!, command.Amount);

                break;

            case DriveVerb.Lens:
                Lens(command.Amount != 0);

                break;

            case DriveVerb.Overlay:
                Washing(command.Path!);

                break;

            case DriveVerb.Tilt:
                // Degrees in, radians held. The clamp lives here rather than in the grammar because
                // the bounds are the SHELL's -- they come from the ground mesh and from LookAt's
                // up-vector, neither of which Borough.Formats knows anything about.
                _pitch = Mathf.Clamp(
                    Mathf.DegToRad(command.Amount), LowestRadians, HighestRadians);

                Orbit();

                break;

            case DriveVerb.Focus:
                // ⚠ THE DISTANCE IS NOT CLAMPED AND A HAND'S ZOOM IS. Nearest/Furthest are derived
                // from the CITY's span, which is the right leash for a mouse wheel and the wrong one
                // for a review: it stops three kilometres out on a 65-kilometre map. A script is not
                // a hand, and this is the one way to look at the other sixty-two.
                _focus = new Vector3(
                    command.East * MetresPerTile, 0f, -command.North * MetresPerTile);

                if (command.Amount > 0)
                {
                    _distance = command.Amount;
                }

                Orbit();

                break;

            case DriveVerb.Click:
                // 🔴 THE BOUNDS ARE CHECKED HERE BECAUSE THE DRIVEN AIM SKIPS THE RAY THAT USED TO
                // CHECK THEM. Aim() clamps a cursor to the map on its way out of the projection; a
                // script names a Tile and meets none of that, so a click past the edge would have
                // reached Simulation with a coordinate no ground has.
                if (command.East < 0 || command.North < 0
                    || command.East >= CellGrid.WorldTiles || command.North >= CellGrid.WorldTiles)
                {
                    _refused = "that is not on the map.";

                    break;
                }

                _aimed = (new Tiles(command.East), new Tiles(command.North));

                Act(command.Amount != 0);

                break;

            case DriveVerb.Quit:
                Quit();

                break;
        }

        // Pause is a rung rather than a separate state (01 §1), so it remembers what it left.
        if (_rung != 0)
        {
            _resume = _rung;
        }

        // ⚠ RECORDED HERE AND NOWHERE ELSE, which is why a keyboard session records as readily as a
        // socket one: every channel arrives at this method, so the log is complete by construction
        // rather than by three call sites remembering.
        if (_record is not null)
        {
            File.AppendAllText(_record, DriveScript.Spell(command) + "\n");
        }
    }

    /// <summary>Write the frame, and the readout beside it under the same name.</summary>
    /// <remarks>
    /// <para>
    /// ⚠ <b>The picture and the caption are written in ONE act</b>, so they cannot disagree about
    /// which Tick they are of. A frame filed beside a readout taken a moment later is a frame whose
    /// numbers belong to a different city.
    /// </para>
    /// <para>
    /// ⚠ <b>THE VIEWPORT HAS NO PICTURE UNDER <c>--headless</c>.</b> <c>GetTexture()</c> is not null
    /// there -- what is empty is what the dummy renderer has behind its RID, which surfaces as an
    /// engine error and a null out of <c>GetImage()</c>. ***A guard that checks the handle misses an
    /// emptiness that is in what the handle points at***, so the display server is asked first,
    /// where the capability is actually declared. <b>The caption is still written</b>: a run with no
    /// screen can still say what the city was doing.
    /// </para>
    /// </remarks>
    private void Shoot(string path)
    {
        // 🔴 RECOMPOSED BEFORE THE SHUTTER, BECAUSE Draw RAN BEFORE THIS FRAME'S COMMANDS DID.
        // Found by running it: 'pause' then 'shoot' on one Tick wrote a caption reading "speed 1x",
        // which is the rung the frame was composed with and not the one the picture was taken at.
        Draw(_alpha);

        // 🔴 AND THE PICTURE IS TAKEN NEXT FRAME, because recomposing repairs the CAPTION and cannot
        // repair the FRAME: a Control's new text reaches the renderer when the engine draws, which
        // is after _Process returns. Two ForceDraws do not bring it forward. ***The caption said
        // Tick 140 over a picture of Tick 139 for as long as shoot has existed***, and it was
        // invisible because a city one Tick apart looks identical -- it took a tool palette, whose
        // whole content changes on one command, to make the lag legible. See _pendingShot.
        _pendingShot = path;

        // Written now rather than with the picture: this frame's compose is the one the picture will
        // carry, and holding the caption over would be a second reading of a world that has moved.
        Write(Path.ChangeExtension(path, ".txt"));
    }

    /// <summary>Save the frame the engine has just drawn.</summary>
    /// <remarks>
    /// ⚠ <b>THE VIEWPORT HAS NO PICTURE UNDER <c>--headless</c>.</b> <c>GetTexture()</c> is not null
    /// there — what is empty is what the dummy renderer has behind its RID, which surfaces as an
    /// engine error and a null out of <c>GetImage()</c>. ***A guard that checks the handle misses an
    /// emptiness that is in what the handle points at***, so the display server is asked first, where
    /// the capability is actually declared. <b>The caption is already written</b>: a run with no
    /// screen still says what the city was doing.
    /// </remarks>
    private void Capture(string path)
    {
        if (DisplayServer.GetName() == "headless")
        {
            GD.Print($"no picture at Tick {_world.Tick.Raw}: --headless renders nothing.");
        }
        else if (GetViewport().GetTexture()?.GetImage() is { } picture)
        {
            picture.SavePng(Globalize(path));
            GD.Print($"wrote {path} at Tick {_world.Tick.Raw}");
        }
        else
        {
            GD.Print($"no picture at Tick {_world.Tick.Raw}: the viewport rendered nothing.");
        }
    }

    /// <summary>
    /// Write the readout, which is <b>the whole of what a driven run returns without a picture</b>.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>The Tick is on its own first line rather than only inside the readout's prose.</b> The
    /// readout is written to be read by a person at 18pt; this file is read by whatever is driving,
    /// and a caller should not have to parse an em-dash to find out which Tick it got.
    /// </remarks>
    private void Caption(string path)
    {
        Draw(_alpha);
        Write(path);
    }

    /// <summary>
    /// Open the door a live driver knocks on. <b>One client, one line, one reply.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>A SOCKET IS A WALL-CLOCK CHANNEL INTO A SIMULATION WHOSE WHOLE DISCIPLINE IS
    /// DETERMINISM, AND THE OBJECTION IS THE RIGHT ONE.</b> It is answered rather than accepted:
    /// every arriving line is stamped with the Tick it landed on and spelled back out through
    /// <c>--record</c>, so ***the log of an interactive session IS a drive script*** and what
    /// somebody did by hand replays as a batch run.
    /// </para>
    /// <para>
    /// ⚠ <b>Lines are applied on the main thread at a Tick boundary</b>, never where they arrive.
    /// The socket thread only enqueues and then blocks for its reply, so nothing off the main thread
    /// ever touches the world — which is the same rule the renderer already keeps.
    /// </para>
    /// <para>
    /// ⚠ <b>The path is unlinked before binding.</b> A Unix socket outlives the process that made
    /// it, so a shell that crashed leaves a file the next one cannot bind — and the failure reads as
    /// *address in use* rather than as *the last run died*.
    /// </para>
    /// </remarks>
    private bool Listen(string path)
    {
        try
        {
            File.Delete(path);

            _listener = new System.Net.Sockets.Socket(
                System.Net.Sockets.AddressFamily.Unix,
                System.Net.Sockets.SocketType.Stream,
                System.Net.Sockets.ProtocolType.Unspecified);

            _listener.Bind(new System.Net.Sockets.UnixDomainSocketEndPoint(path));
            _listener.Listen(1);
        }
        catch (Exception opening)
        {
            GD.PrintErr($"cannot listen on {path}: {opening.Message}");

            return false;
        }

        _door = path;

        new System.Threading.Thread(Serve) { IsBackground = true }.Start();
        GD.Print($"listening on {path}");

        return true;
    }

    /// <summary>Take a line, hand it to the main thread, wait for what it answers.</summary>
    /// <remarks>
    /// ⚠ <b>Strictly one reply per line, and the read blocks until it comes.</b> A driver can
    /// therefore send and read in lock step without a clock of its own — which is the only way a
    /// client can know that what it reads is the state <em>after</em> what it sent.
    /// </remarks>
    private void Serve()
    {
        while (_listener is not null)
        {
            try
            {
                using System.Net.Sockets.Socket client = _listener.Accept();
                using var stream = new System.Net.Sockets.NetworkStream(client);
                using var reader = new StreamReader(stream);
                using var writer = new StreamWriter(stream) { AutoFlush = true };

                while (reader.ReadLine() is { } line)
                {
                    _asked.Add(line);
                    writer.WriteLine(_answered.Take());
                }
            }
            catch (Exception)
            {
                // The listener closed under us, or a client left mid-sentence. Neither is this
                // thread's business to report: the run is either ending or fine without a driver.
                return;
            }
        }
    }

    /// <summary>Apply whatever arrived, then say what the city looks like now.</summary>
    /// <remarks>
    /// ⚠ <b>The readout is recomposed before the reply</b>, for <see cref="Caption"/>'s reason: it
    /// was written by the <c>Draw</c> that ran before these commands did. ⚠ <b>An empty line is a
    /// poll</b> — no commands, and the state comes back anyway, which is how a driver watches
    /// without touching anything.
    /// </remarks>
    private void Answer()
    {
        if (_listener is null)
        {
            return;
        }

        while (_asked.TryTake(out string? line))
        {
            DriveScriptResult read = DriveScript.Line(line, _world.Tick.Raw);

            if (read.Commands is null)
            {
                _answered.Add("refused\t" + read.Describe().Replace('\n', ' '));

                continue;
            }

            foreach (DriveCommand command in read.Commands)
            {
                Apply(command);
            }

            Draw(_alpha);

            _answered.Add(string.Create(
                CultureInfo.InvariantCulture,
                $"ok\t{_world.Tick.Raw}\t{_readout.Text.Replace('\n', '\t')}"));
        }
    }

    /// <summary>
    /// Write <b>everything on screen, as data</b> — one row per drawn instance.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>THIS IS THE ONLY QUESTION THE SHELL CAN ANSWER THAT THE HEADLESS RUNNER CANNOT.</b>
    /// Every number in the readout comes from the core — <c>VisibleAgents.In</c> and the tables
    /// under it — so a driven shell reporting world state reports what <c>Borough.Headless</c>
    /// already reports. ***What only exists here is the DERIVATION***: the transform and the colour
    /// this shell decided on. <c>plans/0045</c> holds the defect that makes the case:
    /// <c>Basis.Scaled</c> scales in the parent frame, so every east–west Segment drew 8 m long and
    /// its own length wide, and <b>half the road network was missing in a picture nobody could
    /// assert on</b>. A row saying a Segment is 8 long and 128 wide fails a test.
    /// </para>
    /// <para>
    /// ⚠ <b>The geometry is read back off the <c>MultiMesh</c> and not from the iterator that filled
    /// it.</b> A dump built by walking the tables a second time would be a second derivation, free
    /// to agree with the world while disagreeing with the picture — which is the entire class of
    /// defect this exists to catch. ***The mesh is what the GPU is handed, so the mesh is what is
    /// asked.***
    /// </para>
    /// <para>
    /// ⚠ <b>Identity comes the other way, from the fill</b>, because a <c>MultiMesh</c> knows only
    /// an instance index and an index is a recycled slot rather than a name. Two layers know an
    /// entity; the rest draw ground and honestly say <c>-</c>.
    /// </para>
    /// <para>
    /// ⚠ <b><c>instances</c> against <c>capacity</c> is not bookkeeping.</b> A layer at capacity has
    /// silently dropped whatever did not fit, and the picture shows a smaller city with nothing to
    /// say so — the one failure a screenshot renders as success.
    /// </para>
    /// </remarks>
    private void DrawList(string path)
    {
        // Recomposed for Caption's reason: Draw ran before this frame's commands did.
        Draw(_alpha);

        var text = new System.Text.StringBuilder();

        text.Append(CultureInfo.InvariantCulture, $"tick\t{_world.Tick.Raw}\n");
        text.Append(
            CultureInfo.InvariantCulture,
            $"ruleset\t{Path.GetFileName(_rulesetPath)}\n");
        text.Append("# layer\tname\tinstances\tcapacity\tvisible\n");

        foreach ((string name, MultiMeshInstance3D layer, bool _, List<ulong>? _) in Layers())
        {
            text.Append(
                CultureInfo.InvariantCulture,
                $"layer\t{name}\t{layer.Multimesh.VisibleInstanceCount}\t"
                    + $"{layer.Multimesh.InstanceCount}\t{(layer.Visible ? 1 : 0)}\n");
        }

        // 🔴 UNDER --headless EVERY TRANSFORM READS BACK AS THE IDENTITY, AND THE FILE STILL LOOKS
        // RIGHT. The counts above are CPU-side and stay true, so a headless dump came out with the
        // correct number of rows, the correct layers and 21,504 lines of zeros -- ***structurally
        // valid and silently wrong***, which is the one failure that survives being eyeballed.
        // ⚠ It is the same shape as the screenshot's headless defect that plans/0045 records twice:
        // the handle is fine and the emptiness is in what the handle points at. So the rows are
        // WITHHELD rather than written, because a file that exists is a file somebody parses.
        if (DisplayServer.GetName() == "headless")
        {
            text.Append(
                "# rows withheld: --headless renders nothing, and a MultiMesh under the dummy "
                    + "renderer returns the identity for every instance. The counts above are "
                    + "CPU-side and are true. A draw list needs a real display.\n");

            File.WriteAllText(Globalize(path), text.ToString());
            GD.Print($"wrote {path} at Tick {_world.Tick.Raw}, counts only: --headless has no geometry.");

            return;
        }

        text.Append("# row\tlayer\tindex\tid\tx\ty\tz\tsx\tsy\tsz\tyaw\tr\tg\tb\n");

        foreach ((string name, MultiMeshInstance3D layer, bool colours, List<ulong>? ids)
            in Layers())
        {
            MultiMesh mesh = layer.Multimesh;

            for (int at = 0; at < mesh.VisibleInstanceCount; at++)
            {
                Transform3D where = mesh.GetInstanceTransform(at);
                Vector3 size = where.Basis.Scale;
                Color paint = colours ? mesh.GetInstanceColor(at) : default;

                text.Append(
                    CultureInfo.InvariantCulture,
                    $"row\t{name}\t{at}\t{(ids is not null && at < ids.Count ? ids[at].ToString(CultureInfo.InvariantCulture) : "-")}\t"
                        + $"{Figure(where.Origin.X)}\t{Figure(where.Origin.Y)}\t{Figure(where.Origin.Z)}\t"
                        + $"{Figure(size.X)}\t{Figure(size.Y)}\t{Figure(size.Z)}\t"
                        + $"{Figure(where.Basis.GetEuler().Y)}\t"
                        + $"{(colours ? $"{Figure(paint.R)}\t{Figure(paint.G)}\t{Figure(paint.B)}" : "-\t-\t-")}\n");
            }
        }

        File.WriteAllText(Globalize(path), text.ToString());
        GD.Print($"wrote {path} at Tick {_world.Tick.Raw}");
    }

    /// <summary>Every layer, in draw order, with whether it paints per instance and who it is.</summary>
    private (string Name, MultiMeshInstance3D Layer, bool Colours, List<ulong>? Ids)[] Layers() =>
    [
        ("ground", _ground, false, null),
        ("hazard", _hazard, false, null),
        ("water", _water, false, null),
        ("flood", _flood, false, null),
        ("road", _roads, false, null),
        ("cell", _cells, false, null),
        ("plot", _plots, true, _plotIds),
        ("building", _buildings, true, _buildingIds),
        ("roof", _roofs, true, _roofIds),
        ("yard", _yards, true, _yardIds),
        ("tree", _trees, false, null),
        ("rock", _rocks, false, null),
        ("traveller", _travellers, false, _travellerIds),
    ];

    /// <summary>
    /// A number in a row. <b>Fixed, invariant and rounded</b>, so two runs diff rather than differ.
    /// </summary>
    private static string Figure(float value) =>
        value.ToString("0.###", CultureInfo.InvariantCulture);

    /// <summary>Put the readout on disk as it stands. Composed by the caller.</summary>
    private void Write(string path) =>
        File.WriteAllText(Globalize(path), $"tick {_world.Tick.Raw}\n{_readout.Text}\n");

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
    /// Slides the eye across the ground by a screen-space delta. <b>The one place panning happens.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// ⚠ <b>It runs along the EYE's axes and not the world's</b>, because a turn parts the two and a
    /// drag would then go off at an angle. ⚠ <b>Across the ground and never through it</b>, so the
    /// city cannot be lost behind the camera by a careless sweep.
    /// </para>
    /// <para>
    /// <b>Scaled by the standoff rather than by the city</b>, so a close-up pans a street at a time
    /// and a wide shot crosses a district. ***Three callers share it*** — a right-drag, a trackpad
    /// pan gesture and the edge scroll — and they differ only in where the delta comes from.
    /// </para>
    /// </remarks>
    /// <param name="by">Screen-space movement, in pixels, of the kind a drag reports.</param>
    private void Pan(Vector2 by)
    {
        float reach = _distance * 0.002f;
        var right = new Vector3(Mathf.Cos(_yaw), 0f, -Mathf.Sin(_yaw));
        var ahead = new Vector3(-Mathf.Sin(_yaw), 0f, -Mathf.Cos(_yaw));

        _focus += ((right * -by.X) + (ahead * by.Y)) * reach;

        Orbit();
    }

    /// <summary>
    /// Pans while the pointer rests near an edge of the window. <b>The camera control that needs no
    /// button at all.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Every verb holds the left button, so a person mid-edit has no hand free to move the
    /// city.</b> Edge scrolling is the affordance that costs no button, which is why every builder
    /// in the reference set has it.
    /// </para>
    /// <para>
    /// ⚠ <b>The speed RAMPS with depth into the margin</b> rather than switching on, so resting a
    /// cursor just inside the boundary creeps and pushing it to the frame's edge moves properly.
    /// ***A binary edge scroll is unusable at both settings***: fast enough to cross the map is too
    /// fast to stop on a street.
    /// </para>
    /// <para>
    /// 🔴 <b>THE TWO PANELS ARE EXCLUDED, AND BY THEIR OWN RECTANGLES RATHER THAN BY A CONSTANT.</b>
    /// The readout is top-left and the hover bottom-left, both inside the margin, so reading either
    /// would otherwise creep the camera the whole time somebody looked at it — ***the one place a
    /// person deliberately parks the cursor is the one place this must not fire.*** ⚠ <b>The first
    /// spelling guessed a 560×200 corner</b> and killed edge-scrolling north-west entirely, which is
    /// a real direction to want; asking the <c>Control</c> for <c>GetGlobalRect</c> makes the dead
    /// zone exactly the size of the text and no larger, and it cannot drift when a panel grows a line.
    /// </para>
    /// <para>
    /// ⚠ <b>It is skipped while a panel is open</b>, because the tuner and the governing panel are
    /// full of fields somebody is aiming at.
    /// </para>
    /// </remarks>
    /// <param name="delta">Seconds since the last frame, so the pan is a rate and not a per-frame step.</param>
    private void Edge(double delta)
    {
        if (_governing || _tuner.Visible)
        {
            return;
        }

        Rect2 frame = GetViewport().GetVisibleRect();
        Vector2 at = GetViewport().GetMousePosition();

        // Outside the window entirely: a pointer that has left is not pointing at an edge, and
        // following it would pan for as long as the cursor was somewhere else.
        if (!frame.HasPoint(at))
        {
            return;
        }

        // Over a panel: reading is not an instruction to move. ⚠ EVERY Control's own rect is asked
        // rather than a guessed corner -- a guess killed edge-scrolling north-west outright on the
        // first attempt -- and the palette is the third, sitting on the bottom edge where the pan
        // would otherwise fight the buttons.
        if (_readout.GetGlobalRect().HasPoint(at)
            || _hover.GetGlobalRect().HasPoint(at)
            || _palette.GetGlobalRect().HasPoint(at))
        {
            return;
        }

        float push = 0f;
        var by = Vector2.Zero;

        Ramp(at.X, frame.Size.X, ref by, ref push, Vector2.Right);
        Ramp(at.Y, frame.Size.Y, ref by, ref push, Vector2.Down);

        if (push > 0f)
        {
            // A rate in pixels a second, converted to the delta Pan wants.
            Pan(by * (float)delta * EdgePixelsPerSecond);
        }
    }

    /// <summary>One axis of the edge ramp: how far into the margin, and which way that points.</summary>
    private static void Ramp(
        float at, float span, ref Vector2 by, ref float push, Vector2 axis)
    {
        if (at < EdgeMarginPixels)
        {
            float depth = (EdgeMarginPixels - at) / EdgeMarginPixels;

            by += axis * depth;
            push += depth;
        }
        else if (at > span - EdgeMarginPixels)
        {
            float depth = (at - (span - EdgeMarginPixels)) / EdgeMarginPixels;

            by -= axis * depth;
            push += depth;
        }
    }

    /// <summary>
    /// Change the eye's standoff rather than the field of view: a zoom that moves the eye keeps
    /// the perspective the picture was framed with. One step is a wheel notch, and a gesture
    /// arrives as a stream of fractions of one.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>A step is a RATIO and not a distance.</b> A fixed distance a notch is the same nudge
    /// over the whole city as it is over one street, so the last notches in cross the ground and
    /// come out underneath it — which is what the bounds here are for.
    /// </remarks>
    private void Dolly(float steps)
    {
        _distance = Mathf.Clamp(
            _distance * Mathf.Pow(DollyPerStep, steps), Nearest(), Furthest());

        Orbit();
    }

    /// <summary>
    /// <c>--ruleset PATH</c>, <c>--citizens N</c>, <c>--start-at TICK</c>, <c>--govern</c>,
    /// <c>--drive PATH</c>, <c>--quit-at TICK</c>, <c>--listen PATH</c> and <c>--record PATH</c>,
    /// after Godot's <c>--</c>.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>A shell reads the command line and the core does not.</b> Every string here is this
    /// project's (<c>adr/0002</c>), and a bad one is reported rather than defaulted, because a
    /// silently-substituted world is a picture of somewhere else.
    /// </remarks>
    private static (string Ruleset, int Citizens, ulong StartAt, bool Govern, string? Drive,
        ulong QuitAt, string? Listen, string? Record) Arguments()
    {
        string ruleset = "rulesets/minimal.toml";
        int citizens = 1_000;
        ulong startAt = 0;
        string? drive = null;
        ulong quitAt = 0;
        string? listen = null;
        string? record = null;
        string[] given = OS.GetCmdlineUserArgs();

        // ⚠ A FLAG AND NOT A PAIR, so it is read over the whole array rather than inside the loop
        // below, which stops one short to read a value after each name.
        bool govern = Array.IndexOf(given, "--govern") >= 0;

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

        return (ruleset, citizens, startAt, govern, drive, quitAt, listen, record);
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

        Fill(_travellers, Travellers(moving), _travellerIds);
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
        int block = _world.Roads.Streets.BlockTiles;

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
            bool horizontal = block > 0 && lots.North[lot].Raw % block == 0;

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
            bool pitched = ((shape >> 32) & 3u) != 0u && tall <= PitchCeilingMetres;

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
            float rise = Mathf.Min(
                slope * (RoofRiseLow
                    + (((shape >> 40) & 0xFFu) / 255f * (RoofRiseHigh - RoofRiseLow))),
                RoofRiseCeilingMetres);

            // 🔴 THE EAVES ARE WHY A PITCH READS AT ALL. A roof cut flush with its own wall meets
            // it as a colour change and nothing else, so every slope looked like the same slope
            // from the camera this game is played at. An overhang puts a line and a shadow at the
            // junction, and the line is the thing the eye measures the angle against.
            Basis cap = Basis.FromScale(
                new Vector3(slope + (EavesMetres * 2f), rise, ridge + (EavesMetres * 2f)));

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
            Basis capped = horizontal != crossed ? Quarter * cap : cap;

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

            // 🔴 A RING IS FOUR WINGS AND NOT ONE BOX (plans/0053 step 4). BuildingPlan.Hollow is
            // the same call LotTable.FloorTiles subtracts, so the courtyard the city counted OUT of
            // this Building's capacity is the courtyard the picture leaves open. ***A Building that
            // counted a hole and drew a solid roof would be the parcel-against-footprint defect
            // arriving one level in***, which is what plans/0052 stage 1 was.
            if (BuildingPlan.Hollow(footWide, footDeep, out int holeWide, out int holeDeep))
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
                    draw,
                    pitched))
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
                pitched,
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
        float draw,
        bool pitched)
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

            float rise = Mathf.Min(
                slope * (RoofRiseLow
                    + (((shape >> 40) & 0xFFu) / 255f * (RoofRiseHigh - RoofRiseLow))),
                RoofRiseCeilingMetres);

            Basis cap = Basis.FromScale(
                new Vector3(slope + (EavesMetres * 2f), rise, ridge + (EavesMetres * 2f)));

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
                    ridgeEastWest ? Quarter * cap : cap,
                    new Vector3(east, tall + (rise * 0.5f), -north)),
                Transform3D.Identity,
                paint,
                slate,
                new Color((faceEast + 1f) * 0.5f, (-faceNorth + 1f) * 0.5f, lit, draw),
                pitched,
                false);
        }
    }

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
        bool Pitched,
        bool Outhoused);

    /// <summary>Fills the body layer and the roof layer from one walk. Returns the Buildings.</summary>
    /// <remarks>
    /// <para>
    /// ⚠ <b>THREE COUNTS AND THE RETURN IS NONE OF THEM.</b> A flat-roofed Building writes no roof
    /// instance, so the roof count is short; and since <c>plans/0053</c> step 4 a courtyard Building
    /// writes <b>four</b> bodies, so the body count is long. ***The readout says <em>Buildings</em>
    /// and has to mean Buildings***, so what is returned is the number of distinct ones.
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
        int yards = 0;
        int buildings = 0;
        ulong last = 0;

        _buildingIds.Clear();
        _roofIds.Clear();
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

            if (!one.Pitched || roofs >= _roofs.Multimesh.InstanceCount)
            {
                continue;
            }

            _roofIds.Add(one.Id);
            _roofs.Multimesh.SetInstanceTransform(roofs, one.Roof);

            // ⚠ THE ROOF OF A SHELL IS THE SHELL'S COLOUR AND NOT THE ROOFING. An abandoned
            // Building that kept a warm red roof would read as the liveliest thing on the street.
            _roofs.Multimesh.SetInstanceColor(
                roofs++, one.Paint == Derelict.SrgbToLinear() ? one.Paint : one.Slate);
        }

        _buildings.Multimesh.VisibleInstanceCount = bodies;
        _roofs.Multimesh.VisibleInstanceCount = roofs;
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

    /// <summary>
    /// This Tick's input: whatever the player raised since the last one, and then nothing.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>The queue is CLEARED here and not after the step</b>, because a command that threw would
    /// otherwise be re-sent on the next frame for ever. ⚠ <b>The Ruleset hash is zero, which is what
    /// <c>Step(default)</c> already passed</b> — a non-zero one is a reload instruction
    /// (<c>Simulation</c> compares it against what is in force), and the tuner owns reloads by its
    /// own path.
    /// </remarks>
    private TickInput Ordered()
    {
        if (_queued.Count == 0)
        {
            return default;
        }

        Command[] raised = [.. _queued];

        // 🔴 THE ROAD GRAPH IS LAID ONCE AND A PLAYER CAN CHANGE IT, WHICH IS THE WHOLE DEFECT.
        // Pave() ran in _Ready and on a tuner rebuild and nowhere else, so a Street laid by Connect
        // existed in the world, routed Trips, carried Lots -- and was never drawn. ***The verb
        // looked broken while working perfectly***, which is the worst way for a verb to fail.
        // Flagged rather than re-paved here because nothing has stepped yet: the Command applies at
        // the top of the next Tick, so the Segment does not exist until Step returns.
        foreach (Command command in raised)
        {
            if (command.Kind == CommandKind.Connect)
            {
                _repave = true;
            }

            // ⚠ _world.Tick AND NOT _world.Tick + 1. Ordered() is evaluated as the argument to
            // Step, so the Tick it names is the one about to run and the one these Commands apply
            // at. Recording the Tick after would put every verb one Tick late in the replay -- a
            // divergence that appears at the first command and looks like a simulation bug.
            _log.Append(_world.Tick, command);
        }

        _queued.Clear();

        return new TickInput(raised, 0);
    }

    /// <summary>
    /// Writes the session so far to a <c>.borough</c> file, and prints what would reproduce it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>THIS IS THE ROW THAT MAKES THE OTHER THREE WORTH ANYTHING.</b> A verb that is not in
    /// the log is a state change no replay reproduces and no State Hash divergence explains — the
    /// sentence <c>Populate</c>, <c>Trip</c> and <c>Arrive</c> each already carry in their own
    /// remark, and which nothing in the shell honoured until now.
    /// </para>
    /// <para>
    /// ⚠ <b>It USES the codec and never implements one</b> (<c>adr/0039</c>).
    /// <see cref="InputLogCodec"/> lives in <c>Borough.Formats</c>, which both shells reference and
    /// neither may duplicate — a second writer is a second format the day one of them is fixed.
    /// </para>
    /// <para>
    /// ⚠ <b>The State Hash goes to the console beside the path, and it is the whole point of the
    /// line.</b> The round trip is <em>replay this file to this Tick and get this number</em>, so
    /// printing the path alone would leave the operator to find the number themselves — and a
    /// verification nobody can run is not one.
    /// </para>
    /// </remarks>
    private void Record()
    {
        string path = Path.Combine(
            System.Environment.CurrentDirectory,
            $"session-{_world.Tick.Raw}{InputLogCodec.Extension}");

        using (var writer = new StreamWriter(path))
        {
            InputLogCodec.Write(writer, _log.Build());
        }

        // 🔴 THE RULESET IN FORCE IS NOT ALWAYS THE FILE ON DISK, and a reproduce line that assumed
        // it was would be wrong exactly when somebody had been experimenting. Regenerate() rebuilds
        // the world from EDITED toml held in memory, so after a tuner pass the log names a content
        // hash no file has -- and Replay.Start refuses a catalogue whose opening hash differs, which
        // is the refusal working and an operator with no way to satisfy it. So the edited Ruleset is
        // written out beside the log and the line points at THAT.
        string rules = _rulesetPath;

        if (RulesetFile.HashOfContent(System.Text.Encoding.UTF8.GetBytes(_toml))
            != RulesetFile.HashOf(_rulesetPath))
        {
            rules = Path.ChangeExtension(path, ".toml");
            File.WriteAllText(rules, _toml);
            GD.Print($"the Ruleset in force is tuned and is not {_rulesetPath}; wrote {rules}");
        }

        GD.Print(
            $"wrote {path} — {_log.Build().Count} commands over {_world.Tick.Raw} Ticks.\n"
            + $"  State Hash 0x{_world.HashState():X16}\n"
            + "  reproduce: dotnet run --project src/Borough.Headless -- "
            + $"--log {path} --ruleset {rules} --ticks {_world.Tick.Raw} "
            + $"--hash-every {_world.Tick.Raw}");
    }

    /// <summary>What the current verb is called, and for Zone which permission it paints.</summary>
    private string Holding() => _verb switch
    {
        // ⚠ "SUBDIVIDE" AND NOT "ZONE", because the verb is not a brush. LotSubdivider.Face returns
        // zero on a frontage Frontage has already claimed, so this creates Lots on virgin ground and
        // can never repaint an existing block. ***A label that promised a brush would make the
        // commonest misuse -- clicking a block that already has Lots -- look like a bug.***
        Verb.Zone => _world.Rules.ZoneRules.Length > 0
            ? $"SUBDIVIDE 0x{_world.Rules.ZoneRules[_zoneChoice].Admits:X4} (z cycles)"
            : "SUBDIVIDE — no [[zone_rule]] declared",
        Verb.Connect => "STREET (shift-click bulldozes)",
        Verb.Demolish => "DEMOLISH — abandoned only",
        Verb.Service => _serviceKind != 0
            ? $"SERVICE {_names.Kind(_serviceKind) ?? _serviceKind.ToString()} (s cycles)"
            : "SERVICE — no kind declares `serves`",
        _ => "look",
    };

    // ---- the verbs -------------------------------------------------------------------------------

    /// <summary>
    /// Turns the click into a <see cref="Command"/> and queues it. <b>Nothing here touches the
    /// world.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>THE SHELL ASKS THE CORE WHETHER A COMMAND WOULD BE REFUSED AND NO LONGER RESTATES A
    /// RULE.</b> It used to guard three refusals by re-implementing them —
    /// <c>plans/0012</c> <b>Cause 1</b> by construction, two places storing one rule and the copy is
    /// the one that drifts — and <b>ten</b> belong to the five verbs it issues.
    /// <see cref="Simulation.Refuses"/> answers off the predicate the applier uses, so ***the shell
    /// declines to SEND what the core would refuse***, on the core's own finding rather than on a
    /// paraphrase of it.
    /// </para>
    /// <para>
    /// ⚠ <b>Why declining rather than catching:</b> commands are applied at the top of a Tick, so an
    /// exception out of <c>Apply</c> aborts <c>Step</c> half way and leaves a world no invariant
    /// covers. ***A crash is not the worst outcome of an unguarded click; a half-stepped world
    /// is.***
    /// </para>
    /// <para>
    /// ⚠ <b>What stays the shell's is AIMING, and it is a different question.</b> <em>No Building
    /// stands in this Cell</em> and <em>that is not on the map</em> are the shell failing to resolve
    /// a cursor into an address; the core is never asked, because there is no command to ask about.
    /// ***A rule belongs to the city and an aim belongs to the hand.***
    /// </para>
    /// <para>
    /// ⚠ <b>Zone has no guard and needs none.</b> A block with no Street on any face yields no Lots,
    /// which is <c>02 §2.2</c>'s third rule and the mechanism by which a bad street layout punishes
    /// the player — ***it is an outcome and not a refusal***, and a shell that greyed it out would be
    /// hiding the lesson. <see cref="Simulation.Refuses"/> says so too.
    /// </para>
    /// </remarks>
    /// <param name="inverted">Whether shift was held, which turns <c>Connect</c> into a bulldoze.</param>
    private void Act(bool inverted)
    {
        // 🔴 CLEARED AFTER THE LOOK CHECK AND NOT BEFORE IT, which was found by driving a script
        // with a misspelt tool in it. `hold plough` disarms the hand and says so; clearing first
        // meant the very next click wiped that sentence before anybody could read it, and the run
        // reported nothing wrong at all. ***A refusal a later click erases is a refusal nobody
        // sees***, and looking is the one verb that changes nothing and should erase nothing.
        if (_verb == Verb.Look)
        {
            return;
        }

        _refused = string.Empty;

        if (Aim() is not { } at)
        {
            _refused = "that is not on the map.";

            return;
        }

        switch (_verb)
        {
            case Verb.Zone when _world.Rules.ZoneRules.Length > 0:
                Send(new Command(
                    CommandKind.Zone,
                    at.East,
                    at.North,
                    _world.Rules.ZoneRules[_zoneChoice].Admits));
                break;

            case Verb.Zone:
                _refused = "this Ruleset declares no [[zone_rule]], so there is no permission to paint.";
                break;

            case Verb.Connect:
                Lay(at, inverted);
                break;

            case Verb.Demolish:
                Clear(at);
                break;

            case Verb.Service:
                Raise(at);
                break;

            default:
                break;
        }
    }

    /// <summary>Choose what the next click means, and which one of the tool.</summary>
    /// <remarks>
    /// <para>
    /// ⚠ <b>The tool is resolved from its NAME here and nowhere else.</b> Which verbs this shell
    /// offers is not something <c>Borough.Formats</c> can know, so the grammar carries the word and
    /// this is the only place that knows what the words are — see <see cref="DriveVerb.Hold"/>.
    /// </para>
    /// <para>
    /// ⚠ <b>An unknown tool is a refusal a person reads rather than a silent <c>look</c>.</b> A
    /// script that misspells its tool would otherwise click five times with nothing held and report
    /// a city that ignored it.
    /// </para>
    /// </remarks>
    private void Hold(string tool, int choice)
    {
        _refused = string.Empty;

        switch (tool)
        {
            case "look":
                _verb = Verb.Look;

                break;

            case "zone":
                _verb = Verb.Zone;
                _zoneChoice = _world.Rules.ZoneRules.Length > 0
                    ? Math.Clamp(choice, 0, _world.Rules.ZoneRules.Length - 1)
                    : 0;

                break;

            case "street":
                _verb = Verb.Connect;

                break;

            case "demolish":
                _verb = Verb.Demolish;

                break;

            case "service":
                _verb = Verb.Service;

                // Zero means "the first kind that serves anything", which is what the s key sends
                // the first time it is pressed. A kind id is 1-based (Ruleset.KindCount).
                _serviceKind = choice > 0 && choice <= byte.MaxValue
                    ? (byte)choice
                    : NextService(0);

                break;

            default:
                // 🔴 DISARMED RATHER THAN LEFT AS IT WAS, and this was found by running it: a
                // misspelt tool left the PREVIOUS one held, so the next click acted with a verb
                // the script had not asked for. ***An unrecognised instruction must not leave a
                // loaded hand*** -- looking is the one verb that cannot do damage.
                _verb = Verb.Look;
                _refused = $"there is no tool called '{tool}'. There is look, zone, street, "
                    + "demolish and service.";

                break;
        }

        ShowTools();
    }

    /// <summary>The keyboard's own <c>hold</c>, so a hand's choice records exactly as a script's.</summary>
    private DriveCommand Held(string tool, int choice) =>
        new(_world.Tick.Raw, DriveVerb.Hold, choice, tool);

    /// <summary>
    /// Queues a command the city would accept, or <b>says in words why it would not.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b><c>plans/0045</c> row 15e, and the whole of it is here.</b> Every verb's refusals were
    /// an <c>InvalidOperationException</c> out of Phase 0 — the right artefact for a log, which must
    /// stop rather than diverge from the session it describes, and the wrong one for a person. This
    /// is the one place a <see cref="Command"/> reaches <see cref="_queued"/> from a click, so it is
    /// the one place that has to ask.
    /// </para>
    /// <para>
    /// ⚠ <b>The answer is good for exactly as long as the world stands still, and it does.</b>
    /// <see cref="Ordered"/> drains the queue as the argument to <c>Step</c>, so nothing runs between
    /// the question and the command applying. ***A shell that asked, stepped, and then sent would be
    /// guarding a city that no longer exists.***
    /// </para>
    /// </remarks>
    private bool Send(Command command)
    {
        Refusal refusal = _simulation.Refuses(command);

        if (refusal == Refusal.None)
        {
            _queued.Add(command);

            return true;
        }

        _refused = Sentence(refusal, command);

        return false;
    }

    /// <summary>
    /// A <see cref="Refusal"/> in the player's words — <b>and the shell owns every one of them.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>The core hands back a number and this is where it becomes a sentence</b>, which is
    /// <c>CLAUDE.md</c>'s leak vector stated as a design rather than as a warning: <em>"the real leak
    /// vector is not <c>using Godot;</c> — it is a method that returns a formatted string because a
    /// panel wanted one."</em> A second front end may word these differently or in another language,
    /// and neither is the city's business.
    /// </para>
    /// <para>
    /// ⚠ <b>They are not the exception messages and must not be.</b>
    /// <c>Simulation.Explain</c> writes for whoever is holding a crash artefact and names the ADR,
    /// the successor mechanism and the Ruleset key; these are for somebody who has just clicked and
    /// wants to know why nothing happened. ***Same rule, two readers, two registers.***
    /// </para>
    /// <para>
    /// ⚠ <b>The unmapped arm names the number rather than saying nothing.</b>
    /// <c>src/Borough.Godot</c> is not in <c>Borough.slnx</c>, so no test can assert this table is
    /// complete — and a silent fallback would make a missing sentence look like a click that
    /// worked. <see cref="Borough.Tests"/> has no reach here; the screen is the only reviewer.
    /// </para>
    /// </remarks>
    private string Sentence(Refusal refusal, Command command) => refusal switch
    {
        Refusal.ConnectRoadKindIsNotStreet =>
            "only a Street can be laid by hand — an Arterial is a route rather than one click.",

        Refusal.ConnectWorldHasNoLattice =>
            "this Ruleset states no [roads] block_tiles, so it has no lattice to edit.",

        Refusal.DemolishNoBuildingOnThatTile =>
            "nothing stands on that plot to clear.",

        Refusal.DemolishBuildingIsOccupied =>
            "somebody still lives there. Clearing occupied ground is a compulsory purchase and its "
            + "price is not built, so only abandoned buildings can be cleared.",

        Refusal.ServiceKindNotDeclared =>
            "this Ruleset declares no such building.",

        Refusal.ServiceKindServesNothing =>
            $"{_names.Kind((byte)command.Zone) ?? "that building"} serves nobody, and only a service "
            + "building is placed by hand — everything else is built by the city on land you zone.",

        Refusal.ServiceNoVacantLotOnThatTile =>
            "that plot is taken. A standing building and an abandoned shell both hold one; demolish "
            + "first.",

        Refusal.GovernNoSuchPolicy or Refusal.GovernPolicyNotInThisWorld =>
            "this city has no such policy to govern.",

        Refusal.GovernPolicyHasNoName =>
            "that [[policy]] states no name, and a governed amount is saved against its name — so "
            + "there would be nothing to restore it to after a reload.",

        Refusal.TripRulesetStatesNoTrips or Refusal.TripWorldHasNoLattice
            or Refusal.TripBlockHoldsNobody or Refusal.TripEndpointsAreOneBuilding
            or Refusal.TripOriginHoldsNoCitizen =>
            "nobody can make that journey.",

        Refusal.ArriveNoGateOnThatTile =>
            "no gate stands there, so nobody can arrive through it.",

        Refusal.VerbNotApplied =>
            "that verb is not built yet.",

        _ => $"refused for reason {(ushort)refusal}, which this shell has no sentence for.",
    };

    /// <summary>One Street on the lattice edge leaving the intersection nearest the cursor.</summary>
    /// <remarks>
    /// ⚠ <b>The AXIS is chosen from where inside the block the cursor sits</b>, which is the only
    /// thing a single click can say about a choice between two edges. <c>adr/0077</c> makes a Connect
    /// exactly one Segment on the lattice, so the question is never <em>which route</em> — it is
    /// which of the two edges leaving one corner, and the cursor's own offset answers it.
    /// </remarks>
    private void Lay((Tiles East, Tiles North) at, bool bulldoze)
    {
        // ⚠ A world with no lattice has block 0, and the axis below would divide by it. The CORE
        // refuses that world by name (Refusal.ConnectWorldHasNoLattice) and Send is what reports it
        // -- so this is arithmetic the shell cannot do rather than a rule it is restating.
        int block = _world.Roads.Streets.BlockTiles;

        if (block <= 0)
        {
            Send(new Command(CommandKind.Connect, at.East, at.North, default));

            return;
        }

        int alongEast = ((at.East.Raw % block) + block) % block;
        int alongNorth = ((at.North.Raw % block) + block) % block;

        var payload = new ConnectPayload(
            alongEast >= alongNorth ? StreetAxis.East : StreetAxis.North,
            bulldoze ? ConnectAction.Bulldoze : ConnectAction.Lay,
            RoadKind.Street);

        Send(new Command(CommandKind.Connect, at.East, at.North, payload.Encode()));
    }

    /// <summary>Clears the abandoned Building nearest the cursor, at its own Lot's Tile.</summary>
    /// <remarks>
    /// 🔴 <b>The command names the LOT's Tile and never the cursor's, and that is not a convenience.</b>
    /// <c>Simulation.BuildingOn</c> matches a Lot's coordinate exactly and
    /// <c>ApplyDemolish</c> refuses rather than clearing the nearest — <em>"a mistyped command must
    /// not be indistinguishable from the demolition somebody meant"</em>. A cursor lands on a Tile
    /// that is almost never a Lot's own, so the shell resolves the click to a Building, shows which
    /// one in the hover, and then sends <b>that Building's address</b>. ***The refusal stays exact
    /// and the aim becomes possible.***
    /// </remarks>
    private void Clear((Tiles East, Tiles North) at)
    {
        (int building, int lot, _) =
            NearestIn(at, CellGrid.ToCells(at.East), CellGrid.ToCells(at.North));

        if (building == Rows.NoSlot)
        {
            _refused = "no Building stands in this Cell.";

            return;
        }

        Send(new Command(CommandKind.Demolish, _world.Lots.East[lot], _world.Lots.North[lot]));
    }

    /// <summary>Raises the held service kind on the vacant Lot nearest the cursor in its Cell.</summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>An O(Lots) walk, and it is the shell's second one.</b> There is no Cell index over Lots
    /// — <c>BuildingResidency</c> indexes Buildings, and a Lot is a point on a Segment rather than
    /// ground — so finding a vacant one near a Tile means walking the table. ⚠ <b>It runs on a CLICK
    /// and on a hover in this mode only</b>, never in the ordinary per-frame path, which is the one
    /// thing that keeps it off the same list as <c>Main.Draw</c>'s three whole-table walks
    /// (<c>plans/0013</c>). ***It is still an unpriced walk and the row says so.***
    /// </para>
    /// <para>
    /// ⚠ <b>The command names the LOT's Tile</b>, on <see cref="Clear"/>'s reasoning and
    /// <c>ApplyService</c>'s own: <em>"a school landing on a neighbour's plot because the click
    /// resolved to the first is worse than a refusal."</em>
    /// </para>
    /// </remarks>
    private void Raise((Tiles East, Tiles North) at)
    {
        if (_serviceKind == 0)
        {
            _refused = "this Ruleset declares no kind with a `serves` key, so there is no service to place.";

            return;
        }

        int lot = VacantNear(at);

        if (lot == Rows.NoSlot)
        {
            _refused =
                "no vacant Lot in this Cell. A Lot holding a Building — standing or an abandoned "
                + "shell — is not vacant; demolish first.";

            return;
        }

        // THE FACTORY AND NOT THE CONSTRUCTOR, for the reason Command.Govern's own remark gives:
        // the packing is named in one place rather than spelled at each call site.
        Send(Command.Service(_world.Lots.East[lot], _world.Lots.North[lot], _serviceKind));
    }

    /// <summary>The vacant Lot nearest a Tile within its own Cell, or <see cref="Rows.NoSlot"/>.</summary>
    private int VacantNear((Tiles East, Tiles North) at)
    {
        LotTable lots = _world.Lots;
        Cells east = CellGrid.ToCells(at.East);
        Cells north = CellGrid.ToCells(at.North);
        int nearest = Rows.NoSlot;
        long best = long.MaxValue;

        for (int slot = 0; slot < lots.Rows.SlotCount; slot++)
        {
            if (!lots.Rows.IsLive(slot) || !lots.IsVacant(slot)
                || CellGrid.ToCells(lots.East[slot]) != east
                || CellGrid.ToCells(lots.North[slot]) != north)
            {
                continue;
            }

            long de = lots.East[slot].Raw - at.East.Raw;
            long dn = lots.North[slot].Raw - at.North.Raw;
            long away = (de * de) + (dn * dn);

            if (away < best)
            {
                best = away;
                nearest = slot;
            }
        }

        return nearest;
    }

    /// <summary>The first declared kind that serves a Need, or zero when the file declares none.</summary>
    /// <remarks>
    /// ⚠ <b>Ids run <c>1..KindCount</c></b> (<c>Ruleset.KindCount</c>), so zero is <em>none</em> and
    /// not the first kind — the same encoding <c>ApplyService</c> reads when it refuses an id nothing
    /// declares.
    /// </remarks>
    private byte NextService(byte after)
    {
        int count = _world.Rules.KindCount;

        for (int step = 1; step <= count; step++)
        {
            var kind = (byte)(((after + step - 1) % count) + 1);

            if (_world.Rules.Declares(kind) && _world.Rules.Kind(kind).Serves != Need.None)
            {
                return kind;
            }
        }

        return 0;
    }

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
            + "0 detached / 1 perimeter / 2 back-to-back / 3 courtyard / 4 slab, sparsest to "
            + "densest. Read off the block and not off the height: a rung names a plot ratio, so "
            + "two Buildings of one height can be on different rungs",
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

    /// <summary>What the weather is doing, or nothing at all when it is doing nothing.</summary>
    /// <remarks>
    /// ⚠ <b>Silent on a world with no <c>[disasters]</c></b>, which is every shipped Ruleset but
    /// <c>flooded.toml</c> — a readout that said <c>floods 0</c> for ever would be a permanent line
    /// about an absent mechanism.
    /// </remarks>
    private string Weather(int under)
    {
        if (!_world.Rules.Disasters.Stated)
        {
            return string.Empty;
        }

        int live = _world.Disasters.Rows.LiveCount;
        int ruined = 0;
        int swept = 0;

        for (int slot = 0; slot < _world.Disasters.Rows.SlotCount; slot++)
        {
            if (_world.Disasters.Rows.IsLive(slot))
            {
                ruined += _world.Disasters.Ruined[slot];
                swept += _world.Disasters.Swept[slot];
            }
        }

        return live == 0
            ? "   no flood"
            : $"   FLOOD — {under:N0} Cells under water, {ruined:N0} ruined, {swept:N0} swept";
    }

    /// <summary>The Road Graph, laid once — the lattice is generated before the first frame.</summary>
    private void Pave()
    {
        RoadSegmentTable segments = _world.Roads.Segments;
        RoadNodeTable nodes = _world.Roads.Nodes;
        int drawn = 0;

        for (int slot = 0; slot < segments.Rows.SlotCount && drawn < _roads.Multimesh.InstanceCount;
             slot++)
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
                * Basis.FromScale(
                    new Vector3(RoadWidthMetres, 1f, from.DistanceTo(to)));

            _roads.Multimesh.SetInstanceTransform(
                drawn++, new Transform3D(basis, from.Lerp(to, 0.5f)));
        }

        _roads.Multimesh.VisibleInstanceCount = drawn;
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
        Color colour, Vector3 size, bool perInstance = false, bool casts = true) =>
        Layer(colour, new BoxMesh { Size = size }, perInstance, casts: casts);

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
    private MultiMeshInstance3D Layer(
        Color colour,
        PrimitiveMesh mesh,
        bool perInstance = false,
        string? painted = null,
        bool casts = true)
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

        mesh.Material = material;

        var multi = new MultiMesh
        {
            TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
            UseColors = perInstance,
            UseCustomData = painted is not null,
        };

        multi.Mesh = mesh;
        multi.InstanceCount = 65_536;
        multi.VisibleInstanceCount = 0;

        var node = new MultiMeshInstance3D { Multimesh = multi };

        if (!casts)
        {
            node.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
        }

        AddChild(node);

        return node;
    }

    /// <summary>A light, so the boxes have faces rather than silhouettes.</summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>THE SUN CAST NOTHING UNTIL 2026-09-01, AND A CITY WITHOUT SHADOWS HAS NO GROUND.</b>
    /// Every Building met the grass at a hard seam, so a box read as a sprite pasted over the map
    /// rather than as a thing standing on it — and a roof's pitch, which is entirely a matter of
    /// two planes catching the light differently, was being read off a colour change alone.
    /// ***The lighting was correct and there was nothing under it***, which in a screenshot looks
    /// exactly like flat art.
    /// </para>
    /// <para>
    /// ⚠ <b>The split distances are set against the CITY and not against the map.</b> A directional
    /// shadow fits its cascades into <c>DirectionalShadowMaxDistance</c>, and the default 100 m
    /// puts every cascade inside one block — so at the standoff this game is played at the shadows
    /// would simply stop a few metres from the eye. The map is 65.5 km and framing the whole of it
    /// would spend every cascade on empty ground, so the distance is the neighbourhood a person
    /// actually looks at.
    /// </para>
    /// <para>
    /// ⚠ <b>The sky is a gradient rather than a colour, and it is the ambient source.</b> A flat
    /// background makes the map's edge read as the end of the world; a horizon puts the edge
    /// somewhere. Taking ambient from the same sky is what makes a north wall blue and a roof
    /// bright, which is the second thing a pitch is read by.
    /// </para>
    /// </remarks>
    private void Sun()
    {
        _light = new DirectionalLight3D
        {
            LightEnergy = 1.25f,
            LightColor = new Color(1.0f, 0.96f, 0.88f),
            ShadowEnabled = true,
            DirectionalShadowMode = DirectionalLight3D.ShadowMode.Parallel4Splits,
            // ⚠ THE CASCADES ARE FITTED ROUND THE CAMERA AND A LONG RANGE IS NOT A FREE ONE. The
            // engine's default 100 m puts every split inside one block; the map is 65.5 km and
            // framing it would spend the whole atlas on empty ground and give the city texels
            // metres wide. This is the neighbourhood a person looks at, and past the fade a
            // distant city is unshadowed rather than wrong.
            //
            // ⚠ THE VALUE HERE IS ONLY THE OPENING ONE. Orbit() overwrites it on every camera
            // move, because how much range is worth spending depends on how far away the eye is
            // standing -- see the comment there, which carries the measurement.
            DirectionalShadowMaxDistance = 1_600f,
            DirectionalShadowFadeStart = 0.85f,
            DirectionalShadowSplit1 = 0.06f,
            DirectionalShadowSplit2 = 0.16f,
            DirectionalShadowSplit3 = 0.44f,

            // ⚠ THE OPENING VALUE ONLY. Daylight() sets this every frame from how high the sun is
            // standing, because the artifact it exists to hide is a function of that and of
            // nothing else -- see the comment there, which carries the measurement and the three
            // things that were tried instead and did not work.
            //
            // ⚠ And the faint concentric MOIRÉ over open ground is a different thing and is real:
            // it is one quantisation level, 1/255, and ShadowBias 1.0 erases it. Not taken,
            // because a bias that large detaches a shadow from its own wall to cure something
            // nobody has reported seeing.
            ShadowBlur = 1.1f,
        };

        // ⚠ NOT NOON, AND SINCE plans/0051 ROW 2 NOT A CONSTANT EITHER. The angle set here is the
        // one the first frame is drawn at before Daylight has run; every frame after it comes from
        // the clock. A sun overhead lights every roof identically and leaves no wall in shadow,
        // which is the one lighting angle at which a pitched roof and a flat one look the same --
        // so the arc below is capped well short of the zenith rather than being astronomically
        // right.
        _light.RotateX(-0.78f);
        _light.RotateY(-0.62f);
        _light.SetParam(Light3D.Param.ShadowNormalBias, 1.6f);
        _light.SetParam(Light3D.Param.ShadowBias, 0.06f);
        AddChild(_light);

        _above = new ProceduralSkyMaterial
        {
            SkyTopColor = new Color(0.28f, 0.42f, 0.66f),
            SkyHorizonColor = new Color(0.63f, 0.70f, 0.78f),
            // ⚠ THE LOWER HEMISPHERE IS WHAT THE FRAME IS FULL OF, and that is the camera rather
            // than a mistake: the eye is tipped 35° down and never moves, so the horizon sits
            // above the top of the picture at every zoom the city is looked at from. What stands
            // behind the map's edge is therefore always this, and a dark ground colour reads as
            // the world having a hole cut round it. Haze instead.
            GroundBottomColor = new Color(0.50f, 0.56f, 0.62f),
            GroundHorizonColor = new Color(0.58f, 0.65f, 0.71f),
            SunAngleMax = 12f,
            SunCurve = 0.2f,
        };

        _air = new Godot.Environment
        {
                BackgroundMode = Godot.Environment.BGMode.Sky,
                Sky = new Sky { SkyMaterial = _above },
                AmbientLightSource = Godot.Environment.AmbientSource.Sky,
                AmbientLightSkyContribution = 1f,
                AmbientLightEnergy = 1.05f,

                // ⚠ AN INSTRUMENT AS MUCH AS A LOOK. Occlusion is what puts a Building's own
                // shadow in the corner where it meets the ground and in the gap between two of
                // them, so a terrace reads as a terrace and a detached pair reads as two houses.
                // The radius is a metre and a half, which is the gap the fill factor leaves.
                SsaoEnabled = true,
                SsaoRadius = 2.2f,
                SsaoIntensity = 1.6f,
                SsaoPower = 1.8f,

                // The sun off a wall at this hour is bright enough to clip, and a clipped wall
                // loses exactly the shading the openings are drawn against.
                TonemapMode = Godot.Environment.ToneMapper.Filmic,
                TonemapWhite = 2.2f,
        };

        AddChild(new WorldEnvironment { Environment = _air });
        Daylight();
    }

    /// <summary>How high the sun climbs at noon, in radians — <b>54°, and short of the zenith.</b></summary>
    /// <remarks>
    /// ⚠ <b>It is a look and not a latitude.</b> A sun overhead lights every roof identically and
    /// leaves no wall in shadow, which is the one angle at which a pitched roof and a flat one are
    /// indistinguishable — and the pitch is what <c>plans/0049</c> spent a session varying. So the
    /// arc is capped where walls still catch light, and nowhere in this file is a claim about where
    /// on Earth the city is.
    /// </remarks>
    private const float NoonRadians = 0.95f;

    /// <summary>The clock fraction the lamps' night opens at, and how much of a Day it spans.</summary>
    /// <remarks>
    /// <para>
    /// <b>This sun rises at 06:00 and sets at 18:00</b> — <c>height</c> is
    /// <c>sin(NoonRadians)·cos(t)</c> and <c>t</c> is <c>(clock − ½)·τ</c>, so it crosses zero at
    /// <c>clock</c> 0.25 and 0.75 and there is no seasonal term. The window below is that night
    /// with a margin at each end: it opens at <b>0.70</b> and closes at <b>0.30</b> — the hour or
    /// so either side that a person would call dusk and dawn.
    /// </para>
    /// <para>
    /// 🔴 <b>THE MARGIN IS WHAT STOPS THE WRAP FROM BEING SEEN.</b> The phase steps from 1 back to
    /// 0 at the instant the window opens, and every lamp is off on both sides of that step — but
    /// only because <c>night</c> has ALSO reached zero by then. At <c>clock</c> 0.70 the sun stands
    /// at height <b>0.25</b>, above the 0.20 where <c>night</c>'s own ramp bottoms out, so the
    /// discontinuity is multiplied by nothing. ***Narrowing this window would put the wrap inside
    /// the lit hours and the whole city would blink once a Day.***
    /// </para>
    /// </remarks>
    private const float NightOpensAt = 0.70f;

    /// <inheritdoc cref="NightOpensAt"/>
    private const float NightSpans = 0.60f;

    /// <summary>Where the sun stands at this Tick, as a unit vector from the city towards it.</summary>
    /// <remarks>
    /// <para>
    /// <b>The whole of the model.</b> <c>t</c> is the hour angle, zero at noon, and the sun rides a
    /// circle through due east and due west tilted so that its high point is due south at
    /// <see cref="NoonRadians"/>. At <c>t = ±π/2</c> it is on the horizon at the two ends of that
    /// circle; at <c>t = π</c> it is as far below the horizon as it was above it. ⚠ <b>The vector
    /// is a unit vector by construction</b> — <c>sin²t + cos²t(sin²m + cos²m)</c> — so nothing here
    /// needs normalising and a rounding error cannot make the day longer.
    /// </para>
    /// <para>
    /// ⚠ <b>+Z is SOUTH in this shell and not north.</b> Every position is built as
    /// <c>(east, height, −north)</c>, so the sun's noon component is <c>+Z</c> and getting that sign
    /// wrong puts the light behind the camera all day, which reads as an overcast world rather than
    /// as a bug.
    /// </para>
    /// <para>
    /// <b>Tick 0 is 05:00</b> (<c>Ticks.DayBeginsAtHour</c>, <c>adr/0101</c> amended), so a run opened
    /// with no <c>--start-at</c> opens on a dawn. That is the clock the city keeps and not something
    /// to correct here; the offset is applied below rather than assumed.
    /// </para>
    /// <para>
    /// 🔴 ⚠ <b>IT TAKES THE SUB-TICK FRACTION, AND THE REASON IS THAT A DAY IS ONLY 2,048 SAMPLES
    /// LONG.</b> One Tick is <b>0.176°</b> of the sun's circle. At 4× the shell steps 32 Ticks a
    /// second and the sweep reads as motion; at <b>1× it steps 8</b>, so a 60 fps frame holds the sun
    /// still for seven frames and then jumps it — and a dawn shadow a hundred metres long snaps
    /// visibly on every one of those jumps. ***The Tick is the simulation's resolution and it is not
    /// the picture's***, which is <see cref="_alpha"/>'s whole argument arriving at the one consumer
    /// that had not asked for it. The city is unmoved: nothing here is read by
    /// <c>Borough.Core</c> and the sun is not state.
    /// </para>
    /// <para>
    /// ⚠ <b>Computed as a fraction of the Day rather than through <c>Ticks.MinuteOfDay</c>.</b> That
    /// helper floors to the whole minute, which at 0.70 minutes a Tick is a <em>second</em>
    /// quantisation laid over the one being removed. The wall-clock offset it carries is applied here
    /// from the same constant, so there is still one place that decides when a Day begins.
    /// </para>
    /// </remarks>
    /// <param name="tick">The Tick the world is on.</param>
    /// <param name="within">How far into that Tick the frame is, in <c>[0, 1)</c>.</param>
    private static Vector3 Sunward(ulong tick, float within)
    {
        float t = (Clock(tick, within) - 0.5f) * Mathf.Tau;

        return new Vector3(
            -Mathf.Sin(t),
            Mathf.Sin(NoonRadians) * Mathf.Cos(t),
            Mathf.Cos(NoonRadians) * Mathf.Cos(t));
    }

    /// <summary>Where in its Day the world is, as a fraction with midnight at zero.</summary>
    /// <remarks>
    /// <para>
    /// Pulled out of <see cref="Sunward"/> so that the sun's angle and the hour the lamps keep are
    /// the same reading of the same clock. <b>Tick 0 is 05:00</b> (<c>Ticks.DayBeginsAtHour</c>),
    /// and applying that offset here is what keeps one place deciding when a Day begins.
    /// </para>
    /// <para>
    /// ⚠ <b>It is not wrapped into <c>[0, 1)</c>.</b> The offset can carry it past 1 and the two
    /// callers both want it that way — the sun takes a cosine, which does not care, and
    /// <see cref="Daylight"/> takes a <c>%</c> of its own against a different origin.
    /// </para>
    /// </remarks>
    /// <param name="tick">The Tick the world is on.</param>
    /// <param name="within">How far into that Tick the frame is, in <c>[0, 1)</c>.</param>
    private static float Clock(ulong tick, float within)
    {
        float ofDay = ((tick % (ulong)Ticks.PerDay) + within) / Ticks.PerDay;

        return ofDay + (Ticks.DayBeginsAtHour / (float)Ticks.HoursPerDay);
    }

    /// <summary>Points the sun, colours the sky and sets the ambient — all from the clock.</summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>THE LIGHT BECOMES THE MOON BELOW THE HORIZON, AND IT IS THE SAME LIGHT.</b> One
    /// <see cref="DirectionalLight3D"/> is aimed from the sun while the sun is up and from the sun's
    /// antipode while it is down — which is a full moon every single night, and is a fib. It is told
    /// here rather than hidden because the alternative is a city nobody can see for half of a
    /// <b>two-minute</b> Day, and because ***a second light would have to be dimmed to nothing at
    /// dawn anyway***, which is this one line spent twice.
    /// </para>
    /// <para>
    /// ⚠ <b>Both ramps start at exactly zero height, which is what stops the swap from popping.</b>
    /// The sun's energy reaches zero as it touches the horizon and the moon's leaves zero from the
    /// same instant, so the 180° flip happens while the light contributes nothing. An earlier
    /// spelling ramped the sun in from below the horizon and the swap was a visible lurch at every
    /// dawn and every dusk. ***What carries the twilight is the AMBIENT and the sky, not the
    /// directional light*** — which is also what happens outdoors.
    /// </para>
    /// <para>
    /// ⚠ <b>The dusk tint is keyed on <c>|height|</c> and therefore paints dawn as well.</b> They are
    /// the same geometry and the shell has no reason to tell them apart.
    /// </para>
    /// </remarks>
    private void Daylight()
    {
        float within = _alpha.Raw / 65_536f;
        Vector3 sunward = Sunward(_world.Tick.Raw, within);
        float height = sunward.Y;
        float up = Mathf.SmoothStep(0f, 0.12f, height);
        float down = Mathf.SmoothStep(0f, 0.10f, -height);

        _light.LookAtFromPosition(height > 0f ? sunward : -sunward, Vector3.Zero, Vector3.Up);

        // 🔴 THE SOFTNESS RIDES THE SUN'S HEIGHT, AND A CONSTANT ONE CANNOT BE RIGHT AT ANY HOUR.
        // A shadow edge is drawn one screen pixel at a time, so a SHALLOW edge holds its row for
        // several pixels before it steps down and the eye reads a comb. How shallow it lies is set
        // by how low the sun is: measured at a 40 m standoff, the tread is four to seven pixels at
        // 08:00 and the comb is invisible by midday. ***That is why the artifact was reported as
        // going away later in the day.***
        //
        // ⚠ A staircase is hidden when the SOFTNESS EXCEEDS THE TREAD, and nothing else hides it.
        // Measured against the same frame, and this is the finding rather than a preference: FXAA
        // moves nothing, PCF filter quality Ultra moves nothing, and 2× supersampling moves
        // nothing -- all three leave the same two-pixel riser, because none of them changes the
        // edge's SLOPE and the slope is what sets the tread. ⚠ Blur is the only lever that
        // reaches it, which is worth stating plainly because blur was tried and rejected once: a
        // FIXED blur is not the fix, and it was measured with the range fixed too, so its screen
        // width swung wildly with the zoom.
        //
        // So it is spent where it is needed and nowhere else. ⚠ THE LAW IS SQUARED AND A LINEAR
        // ONE WAS TRIED FIRST AND MEASURED SHORT: 0.9/height reaches only 2.2 at 08:00, where the
        // sun stands at 0.41, and 2.2 leaves the same two-pixel riser the unblurred edge has. The
        // sun's own height is capped at sin(0.95) = 0.81 here, so the whole usable range is narrow
        // and a gentle curve cannot cross it. Squared, it sits at the 3.5 ceiling through the
        // morning and the evening and eases to 1.1 only around midday, where the edge is steep
        // enough that blur would buy nothing and cost sharpness. The floor on the divisor stops it
        // running away as the sun sets.
        // 🔴 THE FLOOR AND THE CEILING BOTH BIT AT SUNRISE, SO THE LAW WAS FLAT ACROSS THE ONLY
        // HOURS IT EXISTS FOR. Worked at the shipped constants: at 06:45 the sun stands at
        // sin(0.95) x cos(-1.372) = 0.161, the 0.30 floor replaces it, and 0.73 / 0.09 = 8.11 is
        // then clamped to 3.5. ***The ceiling is reached at height 0.457, which is about 09:00***
        // -- so from first light to mid-morning the blur was a CONSTANT 3.5 however raking the sun
        // got, and the staircase it is spent on is worst exactly there. ⚠ The law was correct and
        // it was never reached: two clamps had turned a curve into a plateau over its own domain.
        //
        // The floor comes down to 0.12, which is a sun about twenty minutes up, and the ceiling
        // goes to 6.0. ⚠ A BLUR THIS WIDE IS NOT FREE -- it detaches a shadow from the wall that
        // casts it -- which is why it is spent only where the tread is widest and eases back to
        // 1.1 by midday exactly as before.
        float raking = Mathf.Max(Mathf.Abs(height), 0.12f);

        _light.ShadowBlur = Mathf.Clamp(0.73f / (raking * raking), 1.1f, 6.0f);

        // 🔴 THE SECOND LEVER, AND project.godot NAMED IT WITHOUT ANYONE PULLING IT: the range.
        // Four cascades share one 8192 atlas over DirectionalShadowMaxDistance, so a texel is the
        // range divided by 4096 -- and a texel's footprint on the GROUND is that divided by the
        // sine of the sun's height. At a raking sun the same texel smears six times as far, which
        // is the tread the blur is being asked to hide.
        //
        // ⚠ SO THE RANGE IS TIGHTENED WHERE THE SUN IS LOW rather than the blur alone being
        // widened. Spending the atlas on ground the eye is not on buys nothing; past the fade a
        // distant city is unshadowed rather than wrong, which is the bargain Sun() already struck.
        // ⚠ Orbit() no longer sets this -- it is a function of the standoff AND the hour, and two
        // writers would race whichever ran last.
        _light.DirectionalShadowMaxDistance = Mathf.Clamp(
            _distance * Mathf.Lerp(1.7f, 3f, Mathf.SmoothStep(0.08f, 0.55f, Mathf.Abs(height))),
            300f,
            3_000f);

        // A raking sun is BRIGHTER than a high one and not dimmer -- it is the same disc through
        // more air, and what the air takes is the blue end. The energy peak sits at the horizon for
        // that reason and the colour goes with it.
        float low = 1f - Mathf.SmoothStep(0.02f, 0.30f, height);

        _light.LightEnergy = height > 0f ? Mathf.Lerp(0f, Mathf.Lerp(1.25f, 1.4f, low), up) : down * 0.18f;
        _light.LightColor = height > 0f
            ? new Color(1.0f, 0.96f, 0.88f).Lerp(new Color(1.0f, 0.71f, 0.44f), low)
            : new Color(0.60f, 0.72f, 1.0f);

        // Twilight is wider than the sun's own ramp on purpose: the sky is still lit for a good
        // while after the disc has gone, and an ambient that fell with the sun would black the city
        // out the instant it touched the horizon.
        float lit = Mathf.SmoothStep(-0.26f, 0.22f, height);
        float dusk = 1f - Mathf.SmoothStep(0.02f, 0.40f, Mathf.Abs(height));

        // ⚠ AMBIENT IS THE SKY HERE -- AmbientSource.Sky at full contribution -- so the four colours
        // below are not a backdrop, they are the fill light on every wall the sun is not on. That is
        // why the dusk tint is worth having at all: it puts warm light INTO the shadows, which is
        // the half of golden hour a directional light cannot do.
        _air.AmbientLightEnergy = Mathf.Lerp(0.26f, 1.05f, lit);

        // 🔴 HOW FAR THROUGH THE NIGHT IT IS, AND NOT HOW DARK IT IS. A darkness was sent here
        // first and it was the wrong quantity twice: it is symmetrical, so a window keyed on it
        // said the same thing at nine in the evening and at four in the morning; and it is a
        // brightness, so it reached the emission as a dimmer and faded every lamp in the city up
        // through dusk and down through dawn. ***A lamp is switched, not dimmed.*** The shader now
        // takes an hour and decides for itself, per window, whether that window is lit.
        //
        // ⚠ TAKEN OFF THE CLOCK AND NOT OFF THE SUN'S HEIGHT, because height is a cosine: it barely
        // moves for the hours either side of midnight and then races at the horizon, so a phase
        // built from it would have the whole city going to bed in the first twenty minutes. This is
        // linear in TIME, which is what an hour is.
        //
        // Clamped rather than wrapped, so all day it sits at 1 — past every lamp's hour, which is
        // the same as saying they are all out.
        _paint?.SetShaderParameter(
            "night_phase",
            Mathf.Min(1f, (Clock(_world.Tick.Raw, within) + (1f - NightOpensAt)) % 1f / NightSpans));

        // 🔴 WHICH NIGHT IT IS, so that not every lamp in the city keeps the same hours for ever.
        // Every draw in the shader was keyed on the bay, the storey and the Building -- none of
        // which moves -- so the same windows lit at the same times on every night of the world's
        // life. ***A city you cannot tell two nights apart is a screensaver.***
        //
        // ⚠ IT TURNS OVER AT DUSK AND NOT AT MIDNIGHT. A night spans the date boundary, so a
        // counter incrementing at 00:00 would redraw every lamp halfway through the evening and
        // the city would visibly reshuffle. The offset here is `night_phase`'s own, so the floor
        // steps at the one instant the phase wraps to zero and nothing is keyed on it.
        //
        // ⚠ WRAPPED AT 1024 BECAUSE IT IS A HASH SEED AND NOT A COUNT. A float loses whole numbers
        // past 2^24, and a seed that stops incrementing is a city that stops changing -- which is
        // the defect this exists to fix, arriving quietly a few thousand Days in.
        _paint?.SetShaderParameter(
            "night_index",
            Mathf.Floor(
                (((_world.Tick.Raw % (ulong)(Ticks.PerDay * 1024)) + within) / Ticks.PerDay)
                + (Ticks.DayBeginsAtHour / (float)Ticks.HoursPerDay)
                + (1f - NightOpensAt)));

        _above.SkyTopColor = new Color(0.03f, 0.04f, 0.11f)
            .Lerp(new Color(0.28f, 0.42f, 0.66f), lit)
            .Lerp(new Color(0.24f, 0.26f, 0.52f), dusk * 0.85f);
        _above.SkyHorizonColor = new Color(0.06f, 0.08f, 0.17f)
            .Lerp(new Color(0.63f, 0.70f, 0.78f), lit)
            .Lerp(new Color(1.00f, 0.56f, 0.28f), dusk);
        _above.GroundBottomColor = new Color(0.04f, 0.05f, 0.09f)
            .Lerp(new Color(0.50f, 0.56f, 0.62f), lit);
        _above.GroundHorizonColor = new Color(0.05f, 0.07f, 0.13f)
            .Lerp(new Color(0.58f, 0.65f, 0.71f), lit)
            .Lerp(new Color(0.62f, 0.42f, 0.34f), dusk);
    }

    /// <summary>Fits the eye's orbit to the city that is actually standing.</summary>
    /// <remarks>
    /// ⚠ <b>Framed on the EXTENT and not on the far corner.</b> A lattice may sit at the map's
    /// origin, and a camera pulled back by the largest coordinate then frames the empty map rather
    /// than the city standing in one corner of it.
    /// </remarks>
    private void Frame()
    {
        LotTable lots = _world.Lots;
        float east = float.MaxValue;
        float north = float.MaxValue;
        float eastEnd = float.MinValue;
        float northEnd = float.MinValue;

        for (int slot = 0; slot < lots.Rows.SlotCount; slot++)
        {
            if (!lots.Rows.IsLive(slot))
            {
                continue;
            }

            east = Mathf.Min(east, lots.East[slot].Raw * MetresPerTile);
            north = Mathf.Min(north, lots.North[slot].Raw * MetresPerTile);
            eastEnd = Mathf.Max(eastEnd, lots.East[slot].Raw * MetresPerTile);
            northEnd = Mathf.Max(northEnd, lots.North[slot].Raw * MetresPerTile);
        }

        if (east > eastEnd)
        {
            east = north = 0f;
            eastEnd = northEnd = 512f;
        }

        float span = Mathf.Max(256f, Mathf.Max(eastEnd - east, northEnd - north));
        var centre = new Vector3((east + eastEnd) * 0.5f, 0f, -(north + northEnd) * 0.5f);

        _span = span;
        _focus = centre;
        _distance = span * 0.95f;
        _laid = new Rect2(east, north, eastEnd - east, northEnd - north);
        _paved = Paved(_laid);
    }

    /// <summary>The rectangle the Road Graph's Nodes span, in metres — the ground that is paved.</summary>
    /// <remarks>
    /// ⚠ <b>It falls back to the Lots rather than to nothing</b>, because a Ruleset stating no
    /// <c>[roads]</c> has no Nodes at all and the caller would otherwise get an empty rectangle that
    /// suppresses nothing — which is the same defect as suppressing everything, pointing the other
    /// way.
    /// </remarks>
    private Rect2 Paved(Rect2 fallback)
    {
        RoadNodeTable nodes = _world.Roads.Nodes;
        float east = float.MaxValue;
        float north = float.MaxValue;
        float eastEnd = float.MinValue;
        float northEnd = float.MinValue;

        for (int slot = 0; slot < nodes.Rows.SlotCount; slot++)
        {
            if (!nodes.Rows.IsLive(slot))
            {
                continue;
            }

            east = Mathf.Min(east, nodes.East[slot].Raw * MetresPerTile);
            north = Mathf.Min(north, nodes.North[slot].Raw * MetresPerTile);
            eastEnd = Mathf.Max(eastEnd, nodes.East[slot].Raw * MetresPerTile);
            northEnd = Mathf.Max(northEnd, nodes.North[slot].Raw * MetresPerTile);
        }

        return east > eastEnd ? fallback : new Rect2(east, north, eastEnd - east, northEnd - north);
    }

    /// <summary>
    /// Frames the city and creates the eye. <b>Called once</b> — a regenerate re-frames with
    /// <see cref="Frame"/> and keeps the camera, so a rebuilt world does not arrive with a second
    /// <see cref="Camera3D"/> in the tree and the viewer's own yaw survives the rebuild.
    /// </summary>
    private void Look()
    {
        Frame();

        _lens = new CameraAttributesPractical();
        _camera = new Camera3D { Far = 200_000f, Fov = 60f, Attributes = _lens };

        AddChild(_camera);
        Orbit();
    }

    /// <summary>Puts the lens on or takes it off — the HUD and the depth of field together.</summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>THE HUD IS THE HALF THAT MATTERS AND THE CURSOR IS THE WORST OF IT.</b> The pick marker
    /// is a Cell-sized quad — 128 m — so at the framings a person would photograph from it is a
    /// sixth of the picture in flat yellow. It is exactly right as an instrument and ruinous as a
    /// subject, which is the whole shape of this verb: ***nothing here is wrong, it is just not what
    /// a photograph is of.***
    /// </para>
    /// <para>
    /// ⚠ <b>The focus distance is the ORBIT's and not a number.</b> The eye is always exactly
    /// <see cref="_distance"/> from what it is looking at, so the plane in focus is known for free
    /// and no key has to be authored for it. The near and far bounds are fractions of that, which is
    /// what makes the effect scale: at 300 m it is a tilt-shift over a few streets, and at 3 km it
    /// is a haze over the far half of the city.
    /// </para>
    /// <para>
    /// ⚠ <b>It re-applies on every zoom and every tilt</b> — <see cref="Orbit"/> calls it — because
    /// a focus plane pinned to a distance the camera has since left is a blur over the subject.
    /// </para>
    /// <para>
    /// ⚠ <b>The Cell lattice goes off and does NOT come back</b>, which is the one place this verb
    /// is not its own inverse. Turning it back on would override a person who had switched it off
    /// themselves, and the alternative — remembering what it was — is a second piece of state for a
    /// grid one keypress restores. ***Asymmetric on purpose, and small.***
    /// </para>
    /// </remarks>
    private void Lens(bool on)
    {
        _photographing = on;
        _hud.Visible = !on;
        _cursor.Visible = !on;
        _cells.Visible = _cells.Visible && !on;

        _lens.DofBlurNearEnabled = on;
        _lens.DofBlurFarEnabled = on;
        _lens.DofBlurAmount = 0.12f;
        _lens.DofBlurNearDistance = _distance * 0.62f;
        _lens.DofBlurNearTransition = _distance * 0.34f;
        _lens.DofBlurFarDistance = _distance * 1.34f;
        _lens.DofBlurFarTransition = _distance * 0.7f;
    }

    /// <summary>
    /// Put the eye where the yaw, the pitch and the distance say, looking at the focus.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>The eye is derived from the orbit and never read back out of it.</b> Every verb moves
    /// one of the three fields and calls this, so a rotation cannot drift the framing and a zoom
    /// cannot leave the camera pointing somewhere a pan did not put it.
    /// </remarks>
    private void Orbit()
    {
        float flat = Mathf.Cos(_pitch) * _distance;

        _camera.LookAtFromPosition(
            _focus + new Vector3(
                Mathf.Sin(_yaw) * flat, Mathf.Sin(_pitch) * _distance, Mathf.Cos(_yaw) * flat),
            _focus,
            Vector3.Up);

        // 🔴 THE SHADOW RANGE RIDES THE STANDOFF TOO, and it is the second half of the fix for a
        // crawling shadow edge. A cascade is a fixed count of texels spread over whatever distance
        // it is told to cover, so a range set for the widest shot spends most of its resolution on
        // ground the eye is nowhere near — and at a close orbit the edge under the nose is
        // quantised to texels several screen pixels across. Measured against the same burst: at a
        // 140 m standoff, 1,600 m leaves a fuzzy two-to-three-pixel serration and 400 m leaves a
        // clean line. Tying the two keeps both ends: near work gets tight cascades, and a wide
        // shot still has shadows out to the horizon it can see.
        //
        // ⚠ The floor exists because a very close orbit must still shadow the whole block, and the
        // ceiling because past it the cascades are coarse again and nothing is gained.
        //
        // ⚠ HOW THE CRAWL WAS MEASURED, because it is not a thing a still can show: twenty-four
        // frames one Tick apart at half a Tick a second -- slow enough that the sub-Tick sun left
        // over in each shot is a fiftieth of a Tick rather than a whole one -- tracking one edge.
        // It should advance +3.4 px a Tick. Before the atlas key was repaired and the range tied
        // here, it advanced between -2.4 and +9.5 and WENT BACKWARDS ON SIX OF TWENTY-THREE
        // FRAMES; after, the residual span is 2.9 px against 8.6 and not one step reverses.
        // ***A shadow edge that reverses is what could not be described from the chair.***
        // ⚠ THE RANGE MOVED TO Daylight() AND IS NOT SET HERE, because it is a function of the
        // standoff and of the sun's height together -- see the comment there, which carries the
        // texel arithmetic. Daylight runs every frame and this runs on a camera move, so a copy
        // here would win for exactly one frame and then lose, which is the worst of both.

        // The focus plane rides the standoff, so a zoom or a tilt cannot leave the subject blurred.
        if (_photographing)
        {
            Lens(true);
        }
    }

    /// <summary>Where a step of the tilt key lands, in whole degrees, already clamped.</summary>
    /// <remarks>
    /// ⚠ <b>It rounds before it steps, so the ladder is the same going up and coming back down.</b>
    /// Stepping the radian and converting afterwards would leave 35.26° at 40.26°, 45.26° and so on
    /// — a ladder whose rungs depend on where it was started, which is exactly what an absolute
    /// verb exists to avoid.
    /// </remarks>
    private int Tipped(int degrees) => Mathf.Clamp(
        (int)Mathf.Round(Mathf.RadToDeg(_pitch)) + degrees,
        (int)Mathf.Round(Mathf.RadToDeg(LowestRadians)),
        (int)Mathf.Round(Mathf.RadToDeg(HighestRadians)));

    /// <summary>The nearest the eye may stand, which is close enough to read one Building.</summary>
    private float Nearest() => Mathf.Max(32f, _span * 0.02f);

    /// <summary>The furthest, which is the whole city and a margin, never the whole map.</summary>
    private float Furthest() => _span * 3f;

    /// <summary>The panel, which is every string a human reads (<c>adr/0002</c>).</summary>
    private void Readout()
    {
        _readout = new Label
        {
            Position = new Vector2(16f, 12f),
            LabelSettings = new LabelSettings { FontSize = 18 },

            // ⚠ STATED RATHER THAN INHERITED. A Label defaults to Ignore, so this changes nothing
            // today -- and a verb that stopped working because somebody set a theme's mouse filter
            // would be indistinguishable from the trackpad defect this file just fixed.
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };

        // BOTTOM LEFT AND GROWING UPWARD, which is what the anchors are for. The stack's LENGTH
        // varies with what is under the cursor -- a built Lot on a floodplain in a District says six
        // lines and open ground says two -- and a top-anchored panel would make every line move
        // whenever the one above it appeared. ***A readout that reflows as you sweep is unreadable
        // even when every line in it is right.***
        _hover = new Label
        {
            AnchorTop = 1f,
            AnchorBottom = 1f,
            GrowVertical = Control.GrowDirection.Begin,

            // ⚠ OFFSETS AND NOT Position, WHICH IS WHAT CLIPPED THE LAST LINE. On an anchored
            // Control, Position writes OffsetLeft/OffsetTop -- so setting it pinned the panel's TOP
            // to the window's bottom edge and every line grew off the screen. The bottom edge is the
            // one that has to be pinned when the stack grows upward.
            OffsetLeft = 16f,
            OffsetBottom = -16f,
            LabelSettings = new LabelSettings { FontSize = 16 },
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };

        _hud = new CanvasLayer();

        _hud.AddChild(_readout);
        _hud.AddChild(_hover);
        AddChild(_hud);
        Tuner(_hud);
        Panels();
    }

    /// <summary>
    /// Builds the two panels that are <b>a reading of the Ruleset</b>, and rebuilds them on a tune.
    /// </summary>
    /// <remarks>
    /// 🔴 <b>THE GOVERNING PANEL WAS BUILT ONCE AND A TUNE MAKES A NEW CITY.</b> Its rows are one per
    /// declared <c>[[policy]]</c>, read at <c>_Ready</c>; <see cref="Regenerate"/> replaces the World
    /// and the Ruleset and left the rows where they were, so a tune that changed the policy set left
    /// a panel addressing positions the new city does not have. ⚠ <b>Since row 15e that is a sentence
    /// rather than a half-stepped Tick</b> — <c>Simulation.Refuses</c> catches it — ***but a panel
    /// showing a Policy the city has not got is still a panel lying about the city.*** The palette
    /// would have acquired the same defect the moment it listed a Zone Rule, so both are built here.
    /// </remarks>
    private void Panels()
    {
        _policyPanel?.QueueFree();
        _palette?.QueueFree();

        Governing(_hud);
        Palette(_hud);
    }

    /// <summary>A backing panel in the shell's one panel style.</summary>
    private static PanelContainer Backed(Vector2 at)
    {
        var backing = new StyleBoxFlat
        {
            BgColor = new Color(0.05f, 0.06f, 0.08f, 0.92f),
            ContentMarginLeft = 14f,
            ContentMarginRight = 14f,
            ContentMarginTop = 10f,
            ContentMarginBottom = 10f,
        };

        var panel = new PanelContainer { Position = at };

        panel.AddThemeStyleboxOverride("panel", backing);

        return panel;
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
    // ---- the tuner ----------------------------------------------------------------------------

    /// <summary>
    /// One number the tuner can turn, named by the table it lives in.
    /// </summary>
    /// <remarks>
    /// <para>
    /// ⚠ <b><see cref="Table"/> is what makes this safe, and it is not decoration.</b>
    /// <c>candidates</c> is a key in <c>[placement]</c> <em>and</em> in <c>[jobs]</c>, and
    /// <c>interval</c> is in both plus <c>[[zone_rule]]</c> — so a rewriter matching on the key
    /// alone would turn two dials when the viewer asked for one, and the second would be invisible.
    /// </para>
    /// <para>
    /// <b>An empty <see cref="Table"/> means the field is the shell's own</b> — population and seed
    /// are arguments to <c>World</c> rather than anything a Ruleset states, and they are on the
    /// panel because they are the two biggest levers on what you are looking at.
    /// </para>
    /// </remarks>
    private readonly record struct Dial(string Table, string Key, string Label);

    /// <summary>
    /// What the tuner exposes. <b>Eight, chosen because each one changes what you SEE.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>THIS IS A TUNER AND NOT AN EDITOR, AND THE BOUNDARY IS THAT IT ONLY TURNS KEYS THE
    /// FILE ALREADY STATES.</b> A key the Ruleset does not mention is shown greyed and is never
    /// written, because inserting one means knowing which table to put it in, whether that table
    /// exists, and what else becomes required when it does — <c>[districts]</c> alone makes four
    /// keys and every Good's price mandatory. ***An editor is a different program and it is the one
    /// this would grow into.***
    /// </para>
    /// <para>
    /// ⚠ <b><c>occupants</c> is written to EVERY <c>[[building]]</c> in the file</b>, which is the
    /// one field here that is not one-to-one. It is coherent — <em>every kind holds N</em> — and
    /// every shipped file already declares the same number for every kind, but a file that varied
    /// them would be flattened by a single turn of this dial.
    /// </para>
    /// </remarks>
    private static readonly Dial[] Dials =
    [
        new("", "citizens", "citizens"),
        new("", "seed", "world seed"),
        new("[roads]", "block_tiles", "block_tiles"),
        new("[lots]", "lots_per_segment", "lots_per_segment"),
        new("[roads]", "arterial_count", "arterial_count"),
        new("[placement]", "candidates", "placement candidates"),
        new("[[building]]", "occupants", "occupants"),
        new("[households]", "car_ownership_percent", "car_ownership_percent"),
    ];

    /// <summary>
    /// Reads the value a key currently carries, or <c>null</c> when the file does not state it.
    /// </summary>
    private static string? Stated(string toml, string table, string key)
    {
        string? here = null;

        foreach (string line in toml.Split('\n'))
        {
            string trimmed = line.Trim();

            if (trimmed.StartsWith('['))
            {
                here = trimmed;

                continue;
            }

            if (here == table && Names(trimmed, key, out string value))
            {
                return value;
            }
        }

        return null;
    }

    /// <summary>Whether a line assigns <paramref name="key"/>, and what it assigns.</summary>
    private static bool Names(string line, string key, out string value)
    {
        value = string.Empty;

        int equals = line.IndexOf('=');

        if (equals < 0 || line.StartsWith('#') || line[..equals].Trim() != key)
        {
            return false;
        }

        value = line[(equals + 1)..].Trim();

        return true;
    }

    /// <summary>
    /// The Ruleset text with one key rewritten <b>inside its own table and nowhere else</b>.
    /// </summary>
    private static string Turned(string toml, string table, string key, string to)
    {
        string[] lines = toml.Split('\n');
        string? here = null;

        for (int at = 0; at < lines.Length; at++)
        {
            string trimmed = lines[at].Trim();

            if (trimmed.StartsWith('['))
            {
                here = trimmed;

                continue;
            }

            if (here == table && Names(trimmed, key, out _))
            {
                // The original indentation is kept because these files are column-aligned by hand
                // and a rewriter that reflowed them would make every diff unreadable.
                int equals = lines[at].IndexOf('=');

                lines[at] = string.Concat(lines[at].AsSpan(0, equals + 1), " ", to);
            }
        }

        return string.Join('\n', lines);
    }

    /// <summary>Builds the panel. One row per <see cref="Dials"/> entry, hidden until asked for.</summary>
    private void Tuner(CanvasLayer layer)
    {
        var box = new VBoxContainer();

        // A panel the text can be read against. The readout above is white on whatever the city
        // happens to be, which is legible for three lines and not for twelve rows of numbers.
        var backing = new StyleBoxFlat
        {
            BgColor = new Color(0.05f, 0.06f, 0.08f, 0.92f),
            ContentMarginLeft = 14f,
            ContentMarginRight = 14f,
            ContentMarginTop = 10f,
            ContentMarginBottom = 10f,
        };

        _tuner = new PanelContainer { Visible = false, Position = new Vector2(14f, 108f) };

        _tuner.AddThemeStyleboxOverride("panel", backing);

        _fields = new LineEdit[Dials.Length];

        for (int at = 0; at < Dials.Length; at++)
        {
            var row = new HBoxContainer();
            var name = new Label
            {
                Text = Dials[at].Label,
                CustomMinimumSize = new Vector2(210f, 0f),
            };

            string? stated = Dials[at].Table.Length == 0
                ? Own(Dials[at].Key)
                : Stated(_toml, Dials[at].Table, Dials[at].Key);

            var field = new LineEdit
            {
                Text = stated ?? "—",
                Editable = stated is not null,
                CustomMinimumSize = new Vector2(110f, 0f),
            };

            _fields[at] = field;

            row.AddChild(name);
            row.AddChild(field);
            box.AddChild(row);
        }

        var apply = new Button { Text = "regenerate  (enter)" };

        apply.Pressed += Regenerate;
        box.AddChild(apply);

        _tunerStatus = new Label { Text = "tab closes. a regenerate is a NEW city, not a reload." };
        box.AddChild(_tunerStatus);

        _tuner.AddChild(box);
        layer.AddChild(_tuner);
    }

    /// <summary>
    /// The governing panel: every declared <c>[[policy]]</c>, its amount, and a way to set it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>NOT the tuner, and the separation is the decision.</b> The tuner rewrites Ruleset text
    /// and regenerates a <em>new city</em>; this issues a <c>Govern</c> <see cref="Command"/> against
    /// the city that is running, at a Tick, through the door a replay reproduces. ***One edits the
    /// world's premises and the other plays the game.***
    /// </para>
    /// <para>
    /// ⚠ <b>An unnamed <c>[[policy]]</c> is shown and disabled rather than omitted.</b> Its
    /// <c>Ruleset.PolicyKeys</c> entry is zero and <c>ApplyGovern</c> refuses it — a governed amount
    /// is saved state and a name is the only thing that survives a renumbering. ***Omitting the row
    /// would shift every position below it***, and <c>Govern</c> addresses a Policy by exactly that
    /// position.
    /// </para>
    /// <para>
    /// ⚠ <b>The amount is <c>Command.East</c></b>, which is a field whose name says <em>where</em>.
    /// <c>Command.Govern</c> is the factory that says so; the struct is twelve fully-defined bytes
    /// and widening it would re-spell every committed Input Log.
    /// </para>
    /// </remarks>
    private void Governing(CanvasLayer layer)
    {
        _policyPanel = Backed(new Vector2(14f, 108f));
        _policyPanel.Visible = _governing;

        var box = new VBoxContainer();

        box.AddChild(new Label { Text = "GOVERN — p closes. enter sets the amount." });

        // 🔴 THE FIELD IS THE TRANSFER AMOUNT AND NOT THE TAX RATE, and a panel that did not say so
        // would be actively misleading: on levied.toml every row reads `1` while the levy that
        // actually bites is `percent = 10` in the apply rule. Govern writes PolicyTable.Amount and
        // nothing else -- ApplyCount is Ruleset data and is not governable -- so a person turning
        // this dial expecting a rate would change the wrong number and watch nothing happen.
        box.AddChild(new Label
        {
            Text = "what ONE application moves. how many and what share is the [[policy]]'s"
                + " apply rule, which is not governable.",
        });

        PolicyDefinition[] declared = _world.Rules.Policies;

        _policyFields = new LineEdit[declared.Length];

        for (int at = 0; at < declared.Length; at++)
        {
            var row = new HBoxContainer();
            bool governable = _world.Rules.PolicyKey(at) != 0;
            string named = _names.Policy(at) ?? "unnamed";

            row.AddChild(new Label
            {
                Text = governable
                    ? $"{named} — sweeps {declared[at].Subject.ToString().ToLowerInvariant()} "
                        + $"every {declared[at].Interval:N0}"
                    : $"{named} (no name — ungovernable)",
                CustomMinimumSize = new Vector2(340f, 0f),
            });

            var field = new LineEdit
            {
                Text = _world.Policies.AmountOf(at, declared[at]).ToString(),
                Editable = governable,
                CustomMinimumSize = new Vector2(110f, 0f),
            };

            int position = at;

            field.TextSubmitted += _ => Govern(position);
            _policyFields[at] = field;
            row.AddChild(field);
            box.AddChild(row);
        }

        if (declared.Length == 0)
        {
            box.AddChild(new Label { Text = "this Ruleset declares no [[policy]]." });
        }

        _policyStatus = new Label { Text = "a governed amount is saved state and survives a reload." };

        box.AddChild(_policyStatus);
        _policyPanel.AddChild(box);
        layer.AddChild(_policyPanel);
    }

    /// <summary>
    /// The tool palette — <b>every verb on screen at once, and the one you are holding lit up.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b><c>plans/0045</c> row 15g, and the complaint was the player's.</b> Five verbs and two
    /// <em>cycling</em> sub-selections were reported through one line of text. ***A cycle is the
    /// least discoverable control there is***: you cannot see what you are not holding, so a second
    /// <c>[[zone_rule]]</c> was reachable only by somebody who pressed <c>z</c> twice on spec, and a
    /// world's second <c>serves</c> kind the same way.
    /// </para>
    /// <para>
    /// ⚠ <b>A button is the KEY and not a second channel.</b> Every one of them calls
    /// <see cref="Apply(DriveCommand)"/> with the same <c>hold</c> a keypress builds, so a session
    /// driven by the palette records and replays exactly as one driven by the keyboard — which is the
    /// property row 15e's <c>--record</c> repair bought and which a second applier would spend again.
    /// </para>
    /// <para>
    /// ⚠ <b>An unavailable tool is SHOWN AND DISABLED, with the reason in its own label.</b> That is
    /// the governing panel's rule (<c>an unnamed [[policy]] is shown and disabled rather than
    /// omitted</c>) applied to a different list, and for a related reason: ***omitting a verb teaches
    /// that the game has not got it, where the truth is that this Ruleset has not declared for it.***
    /// </para>
    /// <para>
    /// ⚠ <b>The keys stay on the buttons.</b> The readout's key list shrank to what the palette does
    /// not show, and <c>mode</c> stayed on it — a <c>Control</c> is invisible to <c>readout</c>,
    /// <c>shoot</c> and the socket's reply, all of which carry <c>_readout.Text</c>, so ***a driven
    /// run can only check the palette against a line of the readout.***
    /// </para>
    /// </remarks>
    private void Palette(CanvasLayer layer)
    {
        // BOTTOM CENTRE, which is the one edge nothing else claims: the readout is top-left, the
        // hover bottom-left and the two panels open at (14, 108).
        _palette = Backed(Vector2.Zero);
        _palette.AnchorLeft = 0.5f;
        _palette.AnchorRight = 0.5f;
        _palette.AnchorTop = 1f;
        _palette.AnchorBottom = 1f;
        _palette.GrowHorizontal = Control.GrowDirection.Both;
        _palette.GrowVertical = Control.GrowDirection.Begin;
        _palette.OffsetBottom = -16f;

        var box = new VBoxContainer();

        _tools = new HBoxContainer();
        _choices = new HBoxContainer();

        box.AddChild(_tools);
        box.AddChild(_choices);

        foreach ((string key, string tool, string label) in Tools)
        {
            (bool available, string why) = Offers(tool);

            var button = new Button
            {
                Text = available ? $"{label}  ({key})" : $"{label} — {why}",
                Disabled = !available,
                ToggleMode = true,
                CustomMinimumSize = new Vector2(0f, 34f),
            };

            string held = tool;

            // ⚠ Pressed and not Toggled: ShowTools writes ButtonPressed off the world every frame a
            // tool changes, and Toggled fires on a programmatic write as well as a click -- which
            // would make the refresh issue the command it is describing.
            button.Pressed += () => Choose(held);
            _tools.AddChild(button);
        }

        _policiesButton = new Button
        {
            Text = _world.Rules.Policies.Length > 0
                ? "POLICIES  (p)"
                : "POLICIES — no [[policy]] declared",
            Disabled = _world.Rules.Policies.Length == 0,
            ToggleMode = true,
            ButtonPressed = _governing,
            CustomMinimumSize = new Vector2(0f, 34f),
        };

        // ⚠ NOT a hold: it opens a panel rather than choosing what a click means, so it is the one
        // button here that is not a verb. It sits in the row because the player counted it among the
        // five they could not see.
        _policiesButton.Pressed += Govern;

        _tools.AddChild(_policiesButton);
        _palette.AddChild(box);
        layer.AddChild(_palette);

        ShowTools();
    }

    /// <summary>Open or close the governing panel. <b>The key and the palette button share it.</b></summary>
    private void Govern()
    {
        _governing = !_governing;
        _policyPanel.Visible = _governing;
        _policiesButton.ButtonPressed = _governing;

        if (_governing)
        {
            ShowPolicies();
        }
    }

    /// <summary>A palette button, taking the keyboard's own path so the two cannot part company.</summary>
    private void Choose(string tool)
    {
        // The cycle is preserved for the KEY and never for the button: clicking `zone` while holding
        // `zone` must not advance to the next Zone Rule, because the choices are on screen and
        // clicking one of them is how you pick. A button is an absolute; the key is what cycles.
        Apply(Held(
            tool,
            tool switch
            {
                "zone" => _zoneChoice,
                "service" => _serviceKind,
                _ => 0,
            }));
    }

    /// <summary>Whether this Ruleset can supply the tool, and what to say when it cannot.</summary>
    /// <remarks>
    /// ⚠ <b>Read off the Ruleset rather than restated</b>, on row 15e's rule: <c>zone</c> asks the
    /// declared <c>[[zone_rule]]</c> set and <c>service</c> asks <see cref="NextService"/>, which are
    /// the same two facts <see cref="Act"/> tests before it sends anything.
    /// </remarks>
    private (bool Available, string Why) Offers(string tool) => tool switch
    {
        "zone" => (_world.Rules.ZoneRules.Length > 0, "no [[zone_rule]] declared"),
        "service" => (NextService(0) != 0, "no kind declares `serves`"),
        _ => (true, string.Empty),
    };

    /// <summary>
    /// Lights the held tool and lays out its choices. <b>Called whenever a tool is held.</b>
    /// </summary>
    /// <remarks>
    /// ⚠ <b>The second row is rebuilt rather than hidden</b>, because what it holds is a different
    /// list for each tool — Zone Rules for one, service kinds for another, and a sentence for the
    /// three that have no choice to make. A row of hidden children would need one child per choice
    /// of every tool at once, sized by whichever Ruleset is loaded.
    /// </remarks>
    private void ShowTools()
    {
        string holding = _verb switch
        {
            Verb.Zone => "zone",
            Verb.Connect => "street",
            Verb.Demolish => "demolish",
            Verb.Service => "service",
            _ => "look",
        };

        for (int at = 0; at < Tools.Length && at < _tools.GetChildCount(); at++)
        {
            ((Button)_tools.GetChild(at)).ButtonPressed = Tools[at].Tool == holding;
        }

        foreach (Node gone in _choices.GetChildren())
        {
            gone.QueueFree();
            _choices.RemoveChild(gone);
        }

        switch (_verb)
        {
            case Verb.Zone when _world.Rules.ZoneRules.Length > 0:
                for (int at = 0; at < _world.Rules.ZoneRules.Length; at++)
                {
                    // ⚠ THE NAME AND THE PERMISSION WORD BOTH. The word is what a Lot stores and what
                    // decides whether anything is ever built there (ZoneRuleDefinition.Admits), so a
                    // panel showing only a name would hide the one number a misconfigured Ruleset
                    // goes wrong in.
                    Chip(
                        $"{_names.ZoneRule(at) ?? $"zone rule {at}"}  0x"
                            + $"{_world.Rules.ZoneRules[at].Admits:X4}",
                        at == _zoneChoice,
                        "zone",
                        at);
                }

                break;

            case Verb.Service when NextService(0) != 0:
                for (byte kind = 1; kind <= _world.Rules.KindCount; kind++)
                {
                    if (_world.Rules.Declares(kind) && _world.Rules.Kind(kind).Serves != Need.None)
                    {
                        Chip(
                            $"{_names.Kind(kind) ?? kind.ToString()}  "
                                + $"{_world.Rules.Kind(kind).Serves.ToString().ToLowerInvariant()}",
                            kind == _serviceKind,
                            "service",
                            kind);
                    }
                }

                break;

            default:
                _choices.AddChild(new Label { Text = Doing() });

                break;
        }
    }

    /// <summary>One selectable choice under the held tool.</summary>
    private void Chip(string text, bool held, string tool, int choice)
    {
        var button = new Button { Text = text, ToggleMode = true, ButtonPressed = held };

        button.Pressed += () => Apply(Held(tool, choice));
        _choices.AddChild(button);
    }

    /// <summary>What the held tool does, for the tools that have nothing to choose between.</summary>
    /// <remarks>
    /// ⚠ <b>It says what the verb DOES and not what its key is</b>, which is the half the mode line
    /// never had room for. <c>SUBDIVIDE</c> is the standing example — the readout could say the word
    /// and never that clicking a block which already has Lots does nothing at all.
    /// </remarks>
    private string Doing() => _verb switch
    {
        Verb.Zone => "no [[zone_rule]] declared, so there is no permission to paint",
        Verb.Connect => "click lays one Street on the lattice; shift-click bulldozes it",
        Verb.Demolish => "clears an abandoned shell and frees its Lot; occupied ground is refused",
        Verb.Service => "no kind declares `serves`, so there is no service to place",
        _ => "reads the city and changes nothing",
    };

    /// <summary>The five verbs a click can carry, with the key that holds each.</summary>
    /// <remarks>
    /// ⚠ <b>The tool words are the drive grammar's</b> (<see cref="Hold"/>), so the palette, the
    /// keyboard and a script all name a verb the same way and there is one vocabulary rather than
    /// three.
    /// </remarks>
    private static readonly (string Key, string Tool, string Label)[] Tools =
    [
        ("v", "look", "LOOK"),
        ("z", "zone", "SUBDIVIDE"),
        ("x", "street", "STREET"),
        ("b", "demolish", "DEMOLISH"),
        ("s", "service", "SERVICE"),
    ];

    /// <summary>Re-reads every field off the world, so an open panel shows what is in force.</summary>
    private void ShowPolicies()
    {
        PolicyDefinition[] declared = _world.Rules.Policies;

        for (int at = 0; at < _policyFields.Length && at < declared.Length; at++)
        {
            _policyFields[at].Text = _world.Policies.AmountOf(at, declared[at]).ToString();
        }
    }

    /// <summary>Queues a <c>Govern</c> for one Policy, or says why it cannot.</summary>
    private void Govern(int position)
    {
        if (!int.TryParse(_policyFields[position].Text, out int amount))
        {
            _policyStatus.Text = "that is not a whole number.";

            return;
        }

        // ⚠ THE PANEL RESTATED ONE OF Govern'S THREE REFUSALS AND COULD NOT SEE THE OTHER TWO.
        // Simulation.Refuses answers all three off the applier's own predicate, and the panel says
        // whichever one it gave -- so a Policy this world holds no row for is now a sentence rather
        // than a half-stepped Tick.
        if (!Send(Command.Govern(position, amount)))
        {
            _policyStatus.Text = _refused;

            return;
        }

        _policyStatus.Text =
            $"{_names.Policy(position) ?? $"policy {position}"} set to {amount:N0} "
            + $"on Tick {_world.Tick.Raw + 1:N0}.";
    }

    /// <summary>The current value of a field the shell owns rather than the Ruleset.</summary>
    private string Own(string key) => key switch
    {
        "citizens" => _citizens.ToString(),
        "seed" => _seed.ToString(),
        _ => "—",
    };

    /// <summary>
    /// Rewrites the Ruleset text from the panel, re-parses it, and builds a new city from it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>IT GOES BACK THROUGH THE LOADER AND NEVER POKES THE <see cref="Ruleset"/>.</b>
    /// <c>adr/0048</c> puts every one of the loader's refusals at the parse site, so a panel that
    /// set fields on a parsed Ruleset would bypass all of them — and a bad number would surface as
    /// a crash somewhere unrelated, several minutes later, rather than as the sentence naming the
    /// key and the line. ***The round trip through text is what keeps the tuner honest.***
    /// </para>
    /// <para>
    /// ⚠ <b>A REGENERATE IS A NEW CITY AND NOT A RELOAD, and the panel says so.</b> Most of what is
    /// on it is <em>world-creation</em> under <c>CLAUDE.md</c>'s Kind column — the Road Graph is
    /// laid once — so there is no sense in which the standing city could absorb a new
    /// <c>block_tiles</c>. Everything starts again at Tick 0: the State Hash restarts, and nothing
    /// that happened is carried over. <c>adr/0015</c>'s hot reload is a different mechanism for the
    /// <em>tuning</em> half, and it is not this.
    /// </para>
    /// <para>
    /// ⚠ <b>The eye is re-framed and not rebuilt</b>, so the yaw and zoom you were looking with
    /// survive the regenerate. Comparing two cities is the whole point of the panel, and a camera
    /// that jumped home between them would make the comparison useless.
    /// </para>
    /// </remarks>
    private void Regenerate()
    {
        string toml = _toml;
        int citizens = _citizens;
        ulong seed = _seed;

        for (int at = 0; at < Dials.Length; at++)
        {
            if (!_fields[at].Editable)
            {
                continue;
            }

            string typed = _fields[at].Text.Trim();

            if (Dials[at].Table.Length == 0)
            {
                if (Dials[at].Key == "citizens" && int.TryParse(typed, out int wanted))
                {
                    citizens = wanted;
                }
                else if (Dials[at].Key == "seed" && ulong.TryParse(typed, out ulong drawn))
                {
                    seed = drawn;
                }

                continue;
            }

            toml = Turned(toml, Dials[at].Table, Dials[at].Key, typed);
        }

        RulesetLoadResult loaded = RulesetLoader.Parse(toml, Path.GetFileName(_rulesetPath));

        if (loaded.Ruleset is null)
        {
            // The loader's own sentence, verbatim. It names the key and the line, which is the
            // whole reason the rewrite goes through text rather than around it.
            _tunerStatus.Text = loaded.Describe();

            return;
        }

        _toml = toml;
        _names = loaded.Names;
        _citizens = citizens;
        _seed = seed;

        var key = WorldKey.FromSeed(seed);

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
        _log.Append(Ticks.Zero, new Command(CommandKind.Populate, default, default));
        _simulation.Step(new TickInput([new Command(CommandKind.Populate, default, default)], 0));

        // The four layers laid once rather than per frame. Ground is fixed to the map and would
        // survive, but it is re-laid with the others so that "what a rebuild redoes" is one list.
        Ground();
        Skin();
        Scatter();
        Hazard();
        Flood();
        Pave();
        Frame();
        Cells();
        Orbit();

        // 🔴 REBUILT, BECAUSE BOTH PANELS ARE A READING OF THE RULESET AND THIS IS A NEW ONE. The
        // governing panel had one row per declared [[policy]] from _Ready and was never redone, so a
        // tune that changed the policy set left it addressing positions the new city has not got.
        Panels();

        _owed = 0d;
        _tunerStatus.Text =
            $"new city: {citizens:N0} Citizens, seed {seed}, {_world.Lots.Rows.LiveCount:N0} Lots.";
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
