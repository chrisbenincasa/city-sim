using Borough.Core.Space;

namespace Borough.Appearance;

/// <summary>An inclusive range of whole numbers.</summary>
public readonly record struct Bounds(long Low, long High)
{
    public bool Holds(long value) => value >= Low && value <= High;
}

/// <summary>
/// An authored architectural style a Building can be drawn in, and the facts that make a Building
/// eligible for it. An absent condition admits every Building.
/// </summary>
/// <param name="Kinds">The Ruleset kind names this family can draw.</param>
/// <param name="Weight">The family's share of a draw among the eligible families.</param>
/// <param name="Fallback">
/// Drawn only when no other family admits the Building. A fallback carries no conditions.
/// </param>
/// <param name="Model">The asset the shell resolves, or <c>null</c> where none is authored yet.</param>
/// <param name="Zones">Admits a Lot carrying any of these permission bits.</param>
/// <param name="Body">How the family builds its body, or <c>null</c> where the massing draws it.</param>
/// <param name="Paints">The paint schemes a body draws one of, or none where it keeps its model's colours.</param>
/// <param name="Units">The Unit counts this family admits.</param>
/// <param name="Anchored">Admits only Buildings with an anchor Unit, or only those without one.</param>
/// <param name="Parking">The car park forms this family admits.</param>
/// <param name="Corner">Admits only corner Buildings, or only those off the corner.</param>
public sealed record AppearanceFamily(
    string Id,
    string File,
    int Line,
    string[] Kinds,
    int Weight,
    bool Fallback,
    string? Model,
    Bounds? Storeys,
    Bounds? FrontageMetres,
    Bounds? DepthMetres,
    Bounds? RaisedDay,
    BlockPattern[]? Patterns,
    ushort Zones,
    FamilyBody? Body = null,
    PaintScheme[]? Paints = null,
    Bounds? Units = null,
    bool? Anchored = null,
    ParkingForm[]? Parking = null,
    bool? Corner = null)
{
    public bool Admits(in BuildingFacts facts)
    {
        string kind = facts.Kind;
        return Array.Exists(Kinds, k => k == kind)
            && (Storeys is not { } storeys || storeys.Holds(facts.Storeys))
            && (FrontageMetres is not { } frontage || frontage.Holds(facts.FrontageMetres))
            && (DepthMetres is not { } depth || depth.Holds(facts.DepthMetres))
            && (RaisedDay is not { } raised || raised.Holds(facts.RaisedDay))
            && (Patterns is not { } patterns || Array.IndexOf(patterns, facts.Pattern) >= 0)
            && (Zones == 0 || (Zones & facts.Zone) != 0)
            && (Units is not { } units || units.Holds(facts.Units))
            && (Anchored is not { } anchored || anchored == facts.Anchored)
            && (Parking is not { } parking || Array.IndexOf(parking, facts.Parking) >= 0)
            && (Corner is not { } corner || corner == facts.Corner);
    }
}

/// <summary>An sRGB colour, as authored.</summary>
public readonly record struct Paint(byte R, byte G, byte B);

/// <summary>
/// Colours for some of a body's parts, drawn together. A part the scheme does not name keeps its
/// model's colour.
/// </summary>
/// <param name="Weight">The scheme's share of a draw among its family's schemes.</param>
public sealed record PaintScheme(IReadOnlyDictionary<string, Paint> Parts, int Weight);

/// <summary>
/// A Style Preset: the Appearance Families a city is drawn from, chosen by the player at map start.
/// </summary>
/// <param name="EraDays">
/// The length of an era in game Days. Attached Buildings on one block face raised in the same era
/// share a draw, and so share a family.
/// </param>
/// <param name="Attached">The block patterns whose Buildings stand in attached runs.</param>
/// <param name="Runs">How detached houses on one block face group into runs, or <c>null</c> where each draws alone.</param>
/// <param name="Terraces">
/// How many Lots one terrace of attached houses spans, drawn per face and era, or <c>null</c> where a
/// whole face draws together. Only Buildings that terrace families alone admit stand in terraces.
/// </param>
/// <param name="ApartmentRuns">
/// How many Lots one run of apartment Buildings spans, drawn per face side, or <c>null</c> where
/// apartments draw as any other Building does. A Building whose draw falls to an apartment family
/// stands in a run, on attached and detached faces alike, and takes its run's form.
/// </param>
/// <param name="ShopRuns">
/// How many Lots one parade of shop Buildings spans, drawn per face side, or <c>null</c> where shops
/// draw as any other Building does. A parade shares its form, pitch and walls, as an apartment run does.
/// </param>
/// <param name="WorkshopRuns">
/// How many Lots one run of workshop Buildings spans, drawn per face side, or <c>null</c> where workshops
/// draw as any other Building does. A run shares its form, pitch and walls, as a shop parade does.
/// </param>
public sealed record StylePreset(string Name, int EraDays, BlockPattern[] Attached, AppearanceFamily[] Families,
    HouseRuns? Runs = null, Bounds? Terraces = null, Bounds? ApartmentRuns = null, Bounds? ShopRuns = null, Bounds? WorkshopRuns = null);

/// <summary>
/// Runs of neighboring detached houses on one block face and era. Each run draws what its houses
/// share, by weight.
/// </summary>
/// <param name="Alone">The weight of runs whose houses share nothing.</param>
/// <param name="Matched">The weight of runs whose houses share their family and roof pitch.</param>
/// <param name="Copies">The weight of runs whose houses share their family, proportions, materials and paint.</param>
/// <param name="Houses">How many Lots one run spans, drawn per face.</param>
public sealed record HouseRuns(int Alone, int Matched, int Copies, Bounds Houses);

/// <summary>What the houses of one run share.</summary>
public enum RunShare : byte
{
    Alone,
    Matched,
    Copies,
}

/// <summary>What one bay of one storey shows on a wall.</summary>
public enum BayKind : byte
{
    Blank,
    Window,
    Shop,
    Entry,
    Door,
    Roller,
    Stair,

    /// <summary>
    /// A residential entrance. Neighbouring hall bays on the ground storey share one door centred on
    /// them, under a canopy on the street wall. Above the ground a hall bay is a window.
    /// </summary>
    Hall,

    /// <summary>
    /// A door onto a balcony that spans it. On the ground storey it is a garden door with no balcony.
    /// </summary>
    Balcony,
}

/// <summary>
/// An opening's size, and the height of its foot above its storey's floor. A <c>null</c> sill keeps
/// the builder's own.
/// </summary>
public readonly record struct OpeningSize(float Width, float Height, float? Sill);

/// <summary>
/// One storey's bays along a wall. A filling token repeats to take up the bays the fixed tokens
/// leave, shared evenly among the filling tokens. Bays that do not share evenly go in pairs to the
/// outermost fillers, and a last odd one to the middle filler, so a symmetric row stays symmetric.
/// A token of several kinds lays them in turn, so a filling <c>balcony+window</c> alternates.
/// </summary>
public sealed record BayRow(BayKind[][] Tokens, bool[] Fills)
{
    public static readonly BayRow Blank = new([[BayKind.Blank]], [true]);

    /// <summary>The row laid out over <paramref name="bays"/> bays.</summary>
    public BayKind[] Over(int bays)
    {
        int fixedCount = Fills.Count(f => !f);
        int fillers = Tokens.Length - fixedCount;
        int spare = Math.Max(0, bays - fixedCount);
        int[] share = new int[fillers];
        if (fillers > 0)
        {
            Array.Fill(share, spare / fillers);
            int left = spare % fillers;
            for (int outer = 0; left >= 2; outer++, left -= 2)
            {
                share[outer]++;
                share[fillers - 1 - outer]++;
            }

            if (left == 1) share[(fillers - 1) / 2]++;
        }

        var laid = new List<BayKind>(bays);
        int filler = 0;
        for (int i = 0; i < Tokens.Length && laid.Count < bays; i++)
        {
            int count = Fills[i] ? share[filler++] : 1;
            for (int n = 0; n < count && laid.Count < bays; n++) laid.Add(Tokens[i][n % Tokens[i].Length]);
        }

        while (laid.Count < bays) laid.Add(BayKind.Blank);
        return [.. laid];
    }
}

/// <summary>A wall's ground storey and every storey above it.</summary>
public sealed record WallRule(BayRow Ground, BayRow Upper);

/// <summary>
/// How a family builds its body at any size it admits. Metres throughout.
/// </summary>
/// <param name="Library">The authored model whose materials dress the body's parts.</param>
/// <param name="TileMetres">
/// The size one texture tile covers, by part. A part dressed from the texture library takes the
/// texture's own size unless listed here. Other unlisted parts tile per metre.
/// </param>
/// <param name="Materials">The texture library entry that dresses each named part in place of its model's material.</param>
/// <param name="Plant">Rooftop plant units, spaced evenly along the frontage.</param>
/// <param name="Vents">Rooftop vents, spaced evenly along the frontage.</param>
/// <param name="Openings">
/// Opening sizes that replace the builder's own for window, door, stair, shop, roller and balcony
/// bays, on every storey.
/// </param>
/// <param name="GableDegrees">
/// The pitch of a gable roof whose ridge runs along the street, or 0 for a flat roof.
/// </param>
/// <param name="Chimney">A chimney stands behind the ridge near the right-hand end.</param>
/// <param name="Steps">A step stands before each street door.</param>
/// <param name="PartyLine">
/// The body reads as two halves: a party line runs up the middle of the street and back walls and an
/// upstand crosses the roof.
/// </param>
/// <param name="Shopfront">
/// A fascia runs over each wall's ground-storey shops, and an awning shades each run of street shops.
/// </param>
/// <param name="Panels">Panel joints stand at every bay line of the street and back walls, with a floor band at each storey.</param>
/// <param name="Rooflights">Two rooflights and a vent stand over each street bay.</param>
/// <param name="ReceivingCanopy">A canopy shelters each roller door on the back wall.</param>
public sealed record FamilyBody(
    string Library,
    IReadOnlyDictionary<string, (float Along, float Up)> TileMetres,
    IReadOnlyDictionary<string, string> Materials,
    float BayMetres,
    float ParapetMetres,
    bool Pilasters,
    int Plant,
    WallRule Street,
    WallRule Back,
    WallRule Side,
    bool RoofHatch,
    int Vents,
    IReadOnlyDictionary<BayKind, OpeningSize> Openings,
    float GableDegrees,
    bool Chimney,
    bool Steps,
    bool PartyLine = false,
    bool Shopfront = false,
    bool Panels = false,
    bool Rooflights = false,
    bool ReceivingCanopy = false,
    TowerBody? Tower = null,
    MidriseBody? Midrise = null,
    HouseBody? House = null,
    TerraceBody? Terrace = null,
    ApartmentBody? Apartment = null,
    ShopBody? Shop = null,
    WorkshopBody? Workshop = null);

/// <summary>An attached house's roof and plan. Every house of one terrace shares them and its pitch.</summary>
/// <param name="PitchDegrees">The range a terrace's pitch is drawn from, or <c>null</c> for a flat or mansard roof.</param>
public sealed record TerraceBody(TerraceForm Form, Bounds? PitchDegrees);

/// <summary>The roofs and plans an attached house can take.</summary>
public enum TerraceForm : byte
{
    /// <summary>A ridge along the street, stopping at each shared wall.</summary>
    SideGable,

    /// <summary>A flat roof behind a street parapet over a bracketed cornice.</summary>
    Parapet,

    /// <summary>Walls one storey short of the Building, with its top storey inside a mansard lit by dormers.</summary>
    Mansard,

    /// <summary>A ridge running back from the street, so each house shows a gable.</summary>
    FrontGable,

    /// <summary>Two houses under one street gable whose ridge stands on their shared wall.</summary>
    PairedGable,

    /// <summary>A side gable over a front wall set back behind a bay window that reaches the Lot line.</summary>
    BayFront,
}

/// <summary>An apartment Building's roof and plan. Every Building of one run shares them and its pitch.</summary>
/// <param name="PitchDegrees">The range a run's pitch is drawn from, or <c>null</c> for a form without a pitched roof.</param>
public sealed record ApartmentBody(ApartmentForm Form, Bounds? PitchDegrees);

/// <summary>The roofs and plans an apartment Building can take. Each draws the Building's whole frontage.</summary>
public enum ApartmentForm : byte
{
    /// <summary>A flat roof behind a parapet on all four walls.</summary>
    Parapet,

    /// <summary>A ridge along the street, hipped at each free end and stopping at each shared wall.</summary>
    Hipped,

    /// <summary>A ridge along the street, gabled at each free end and stopping at each shared wall.</summary>
    SideGable,

    /// <summary>Walls one storey short of the Building, with its top storey inside a mansard lit by dormers.</summary>
    Mansard,

    /// <summary>A hipped range whose middle third steps forward to the Lot line under a front gable.</summary>
    CentreGable,

    /// <summary>A parapet range set back behind full-height canted bays that reach the Lot line.</summary>
    BayFront,

    /// <summary>Two end wings on the Lot line and the middle set back around a forecourt, under parapets.</summary>
    Forecourt,

    /// <summary>A parapet range whose top storey stands back from the street behind a railed roof terrace.</summary>
    Setback,
}

/// <summary>
/// A shop-house body: shops on the ground storey with flats above. A parade shares its form, pitch and
/// walls, and each shop unit draws its own shopfront style, paint, awning and sign.
/// </summary>
/// <param name="PitchDegrees">The range a parade's pitch is drawn from, or <c>null</c> for a form without a pitched roof.</param>
public sealed record ShopBody(ShopForm Form, Bounds? PitchDegrees);

/// <summary>The period roofs and plans a shop-house Building can take.</summary>
public enum ShopForm : byte
{
    /// <summary>A slate side gable, sash windows, a splayed corner door and a gable end to the side street.</summary>
    VictorianGable,

    /// <summary>A front gable and a first-floor oriel over each shop, under clay tiles.</summary>
    GabledOriel,

    /// <summary>A hipped slate roof over a splayed corner that rises into an octagonal turret.</summary>
    TurretCorner,

    /// <summary>A flat roof behind a corbelled cornice and name panels, with a column under a cut-back corner.</summary>
    MainStreet,

    /// <summary>Stucco walls a storey short of the Building, with its top storey in a slate mansard.</summary>
    Mansard,

    /// <summary>A flat roof behind a parapet, render bands, steel windows and a rounded corner.</summary>
    InterwarCurve,

    /// <summary>A thin flat roof, a concrete canopy over the shops and ribbon windows over panels.</summary>
    SixtiesCanopy,

    /// <summary>A brick base and a zinc-clad top storey set back behind a glazed terrace.</summary>
    SetbackModern,

    /// <summary>Render under a deep-eaved hipped pantile roof, with shutters and balconettes.</summary>
    PantileHipped,

    /// <summary>A detached store glazed between piers under flats, whose top storey of three stands back behind a roof terrace.</summary>
    GlassBase,

    /// <summary>A detached store under steep metal gables across the frontage, glass up each gable end.</summary>
    GabledHall,

    /// <summary>A detached store set back behind square piers, with the flats flush with the street above.</summary>
    Colonnade,

    /// <summary>A detached store under one low mono-pitch rising to the street over a clerestory.</summary>
    MonoPitch,

    /// <summary>A detached store under a deep canopy that turns both corners, ribbon windows over panels above.</summary>
    Canopy,

    /// <summary>A detached store under a folded-plate roof with glass in each fold's front.</summary>
    FoldedPlate,

    /// <summary>A detached brick block: a timber shopfront, sash windows and a corbelled cornice under a name panel.</summary>
    CommercialBlock,
}

/// <summary>
/// A workshop body. A run shares its form, pitch and walls, and each Building draws its own doors,
/// their paint and its signs.
/// </summary>
/// <param name="PitchDegrees">The range a run's pitch is drawn from, or <c>null</c> for a form without a pitched roof.</param>
public sealed record WorkshopBody(WorkshopForm Form, Bounds? PitchDegrees);

/// <summary>The period roofs and plans a workshop Building can take.</summary>
public enum WorkshopForm : byte
{
    /// <summary>The block body's sheet-panel shed under a flat parapet, drawn from its bay rules.</summary>
    PanelShed,

    /// <summary>A hipped brick yard house beside a boarded cart shed on posts, under pantiles.</summary>
    BuildersYard,

    /// <summary>A low side gable with a glazed clerestory monitor along the ridge, between brick piers.</summary>
    Monitor,

    /// <summary>A Belfast-truss bow roof behind a stepped rendered parapet and name panel.</summary>
    InterwarGarage,

    /// <summary>Brown brick under a flat parapet with roller doors and a high strip window.</summary>
    ParapetRoller,

    /// <summary>A low-pitch gable front in profiled sheet over a block dado, with sectional doors and a trade counter.</summary>
    PortalShed,
}

/// <summary>A tower body's authored layout.</summary>
public sealed record TowerBody(TowerVariant Variant);

/// <summary>A mid-rise body's authored type, built over the whole footprint and as a ring when the Lot is hollow.</summary>
/// <param name="ModuleMetres">The window module along a face; <c>null</c> keeps the type's own.</param>
/// <param name="Street">What the upper storeys of a street face open with.</param>
/// <param name="Shops">The street face's ground storey is glazed as shopfronts.</param>
/// <param name="Attic">A mansion's top storey is set back behind a terrace; otherwise a parapet tops it.</param>
/// <param name="Galleries">A panel slab's yard faces carry access galleries; otherwise they have windows.</param>
public sealed record MidriseBody(
    MidriseVariant Variant,
    float? ModuleMetres = null,
    StreetOpenings Street = StreetOpenings.Loggias,
    bool Shops = false,
    bool Attic = true,
    bool Galleries = true);

/// <summary>A house body's form. Every proportion, material and paint is drawn per Building.</summary>
/// <param name="Wide">
/// The form stands among the wide house forms, which share one place in the draw and rank their forms
/// on each run of detached houses so neighboring runs differ in form.
/// </param>
public sealed record HouseBody(HouseVariant Variant, bool Wide = false);

/// <summary>The house forms a body can build.</summary>
public enum HouseVariant
{
    /// <summary>A row of side-gabled houses under one ridge along the street, or one house on a narrow Lot.</summary>
    Terrace,

    /// <summary>A row of houses, each with its own ridge running back from the street, or one on a narrow Lot.</summary>
    FrontGables,

    /// <summary>A hipped block set back behind gabled bays that stand forward to the Lot line.</summary>
    Villa,

    /// <summary>One house under a roof that slopes on all four sides.</summary>
    Hipped,

    /// <summary>One front-gabled house whose gable peaks are clipped by small hips.</summary>
    HalfHip,

    /// <summary>One side-gabled house with a smaller street gable over one side.</summary>
    CrossGable,

    /// <summary>One house whose rear roof slope runs down to a storey lower than its front.</summary>
    Saltbox,

    /// <summary>One house with a storey fewer of wall and its top floor inside a gambrel roof.</summary>
    Gambrel,

    /// <summary>One house whose street face steps back beside a full-height gabled wing.</summary>
    LPlan,

    /// <summary>One house with a single-storey range behind it under a lean-to roof.</summary>
    RearOutshot,

    /// <summary>One house with a single-storey wing standing into its side garden.</summary>
    SideWing,

    /// <summary>A row of steep front gables without eaves, every other house set back from the street.</summary>
    StaggeredGables,

    /// <summary>A flat-roofed row whose top storey stands back behind a roof terrace at three storeys.</summary>
    FlatSetback,

    /// <summary>A row with a mono-pitch to each house, all rising the same way, a clerestory in each step.</summary>
    Sawtooth,

    /// <summary>Front-gabled pavilions joined by flat-roofed links set back from the street.</summary>
    Pavilions,

    /// <summary>A shallow side gable over narrow houses between brick cross walls, panels under ribbon windows.</summary>
    Span,

    /// <summary>A row with an inverted roof to each house, falling to a valley on its center line.</summary>
    Butterfly,

    /// <summary>One low mono-pitch over the row rising toward the street, a clerestory band under its high edge.</summary>
    Monopitch,

    /// <summary>A low hipped roof with deep eaves over the row, the upper storey hung with tiles or boards.</summary>
    LowHip,

    /// <summary>The flat set-back row over a double-pile front, flat-roofed outriggers behind.</summary>
    DeepFlat,

    /// <summary>The staggered gables over a double-pile front, flat-roofed outriggers behind.</summary>
    DeepGables,

    /// <summary>The sawtooth row over a double-pile front, flat-roofed outriggers behind.</summary>
    DeepSawtooth,

    /// <summary>The span row over a double-pile front, flat-roofed outriggers behind.</summary>
    DeepSpan,
}

/// <summary>What a mid-rise body's upper street storeys open with.</summary>
public enum StreetOpenings
{
    Loggias,
    Balconies,
    Windows,
}

/// <summary>The two mid-rise types a body can build.</summary>
public enum MidriseVariant
{
    /// <summary>A brick perimeter block with a set-back attic storey.</summary>
    Mansion,

    /// <summary>A precast panel slab with galleries on the yard side and a recessed ground storey.</summary>
    PanelSlab,
}

/// <summary>The four tower layouts a body can build.</summary>
public enum TowerVariant
{
    Point,
    SteppedPoint,
    L,
    H,
}

/// <summary>The side walls a body shares with a neighbour, left and right as seen from the street.</summary>
[Flags]
public enum AttachedSides : byte
{
    None = 0,
    Left = 1,
    Right = 2,

    /// <summary>The left neighbour runs crosswise, so the roof hips down to it.</summary>
    LeftCrosswise = 4,

    /// <summary>The right neighbour runs crosswise, so the roof hips down to it.</summary>
    RightCrosswise = 8,
}
