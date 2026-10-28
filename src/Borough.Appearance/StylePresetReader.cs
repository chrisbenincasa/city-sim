using System.Globalization;
using Borough.Core.Space;
using Borough.Core.Entities;
using Tomlyn.Parsing;
using Tomlyn.Syntax;

namespace Borough.Appearance;

/// <summary>One problem found in a Style Preset, located in its source file.</summary>
public sealed record AppearanceDiagnostic(string File, int Line, string Message)
{
    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"{File}:{Line}: {Message}");
}

/// <summary>A preset read from its directory, or the errors that refused it.</summary>
public sealed record StylePresetResult(StylePreset? Preset, IReadOnlyList<AppearanceDiagnostic> Errors);

/// <summary>
/// Reads and checks a Style Preset directory. Every <c>*.toml</c> file in it is read in ordinal name
/// order; exactly one declares <c>[preset]</c>, and any may declare <c>[[family]]</c> tables.
/// </summary>
/// <remarks>
/// The reader walks Tomlyn's syntax tree rather than a model so that every refusal carries a line,
/// as <c>RulesetLoader</c> does. It refuses unknown keys rather than ignoring them.
/// </remarks>
public static class StylePresetReader
{
    private static readonly Dictionary<string, BlockPattern> PatternNames = new()
    {
        ["detached"] = BlockPattern.Detached,
        ["perimeter"] = BlockPattern.Perimeter,
        ["back-to-back"] = BlockPattern.BackToBack,
        ["courtyard"] = BlockPattern.Courtyard,
        ["slab"] = BlockPattern.Slab,
        ["tower"] = BlockPattern.Tower,
        ["car-park-centre"] = BlockPattern.CarParkCentre,
        ["shop-house-parade"] = BlockPattern.ShopHouseParade,
        ["supermarket"] = BlockPattern.Supermarket,
        ["decked-supermarket"] = BlockPattern.DeckedSupermarket,
        ["high-street-block"] = BlockPattern.HighStreetBlock,
        ["pad-site"] = BlockPattern.PadSite,
        ["sales-yard"] = BlockPattern.SalesYard,
        ["precinct"] = BlockPattern.Precinct,
        ["gallery-precinct"] = BlockPattern.GalleryPrecinct,
        ["market-hall"] = BlockPattern.MarketHall,
    };

    private static readonly Dictionary<string, ParkingForm> ParkingNames = new()
    {
        ["none"] = ParkingForm.None,
        ["surface"] = ParkingForm.Surface,
        ["deck"] = ParkingForm.Deck,
    };

    private static readonly Dictionary<string, ushort> ZoneNames = new()
    {
        ["housing"] = LotTable.Housing,
        ["trade"] = LotTable.Trade,
    };

    private static readonly string[] Rows = ["street_ground", "street_upper", "back_ground", "back_upper", "side_ground", "side_upper"];

    private static readonly Dictionary<string, BayKind> BayNames = new()
    {
        ["blank"] = BayKind.Blank,
        ["window"] = BayKind.Window,
        ["shop"] = BayKind.Shop,
        ["entry"] = BayKind.Entry,
        ["door"] = BayKind.Door,
        ["roller"] = BayKind.Roller,
        ["stair"] = BayKind.Stair,
        ["hall"] = BayKind.Hall,
        ["balcony"] = BayKind.Balcony,
    };

    private static readonly Dictionary<string, BayKind> SizedOpenings = new()
    {
        ["window"] = BayKind.Window,
        ["door"] = BayKind.Door,
        ["stair"] = BayKind.Stair,
        ["shop"] = BayKind.Shop,
        ["roller"] = BayKind.Roller,
        ["balcony"] = BayKind.Balcony,
    };

    private static readonly string[] Conditions = ["storeys", "frontage_metres", "depth_metres", "raised_day", "patterns", "zones", "units", "anchored", "parking", "corner"];

    private static readonly string[] LowRiseBodyKeys =
    [
        "bay_metres", .. Rows, "openings", "gable_degrees", "chimney", "steps", "pilasters", "party_line", "shopfront",
        "panels", "rooflights", "receiving_canopy", "plant", "vents", "roof_hatch", "parapet_metres", "terrace", "apartment", "workshop", "pitch_degrees",
    ];

    internal static IReadOnlyList<string> BlockOnlyBodyKeys => LowRiseBodyKeys;

    private static readonly string[] MidriseBodyKeys = ["module_metres", "street_openings", "shops", "attic", "galleries"];

    internal static IReadOnlyList<string> MidriseOnlyBodyKeys => MidriseBodyKeys;

    public static StylePresetResult Read(string directory)
    {
        ArgumentNullException.ThrowIfNull(directory);
        var errors = new List<AppearanceDiagnostic>();
        if (!Directory.Exists(directory))
        {
            errors.Add(new AppearanceDiagnostic(directory, 0, "no such Style Preset directory."));
            return new StylePresetResult(null, errors);
        }

        string[] files = Directory.GetFiles(directory, "*.toml");
        Array.Sort(files, StringComparer.Ordinal);
        return Read(files.Select(f => (f, File.ReadAllText(f))));
    }

    public static StylePresetResult Read(IEnumerable<(string File, string Text)> sources)
    {
        ArgumentNullException.ThrowIfNull(sources);
        var errors = new List<AppearanceDiagnostic>();
        var families = new List<AppearanceFamily>();
        (string Name, int EraDays, BlockPattern[] Attached, HouseRuns? Runs, Bounds? Terraces, Bounds? ApartmentRuns, Bounds? ShopRuns, Bounds? WorkshopRuns)? preset = null;
        string? presetAt = null;

        foreach ((string file, string text) in sources)
        {
            int? lastFamily = null;
            bool lastFamilyRefused = false;
            DocumentSyntax document = SyntaxParser.Parse(text, file, validate: true);
            foreach (DiagnosticMessage message in document.Diagnostics)
            {
                errors.Add(new AppearanceDiagnostic(file, message.Span.Start.Line + 1, message.Message));
            }

            if (document.HasErrors) continue;

            foreach (KeyValueSyntax loose in document.KeyValues)
            {
                errors.Add(new AppearanceDiagnostic(file, LineOf(loose), $"'{NameOf(loose.Key)}' must sit inside [preset] or [[family]]."));
            }

            foreach (TableSyntaxBase table in document.Tables)
            {
                var reading = new Reading(file, table, errors);
                switch (NameOf(table.Name), table is TableArraySyntax)
                {
                    case ("preset", false):
                        if (presetAt is not null)
                        {
                            reading.Refuse(LineOf(table), $"[preset] is already declared at {presetAt}.");
                            break;
                        }

                        presetAt = $"{file}:{LineOf(table)}";
                        preset = reading.Preset();
                        break;

                    case ("family", true):
                        AppearanceFamily? family = reading.Family();
                        lastFamilyRefused = family is null;
                        lastFamily = family is null ? null : families.Count;
                        if (family is not null) families.Add(family);

                        break;

                    case ("family.body", false):
                        FamilyBody? body = reading.Body();
                        if (lastFamily is not { } owner)
                        {
                            if (!lastFamilyRefused) reading.Refuse(LineOf(table), "[family.body] must follow the [[family]] it builds.");
                        }
                        else if (families[owner].Body is not null)
                        {
                            reading.Refuse(LineOf(table), $"family '{families[owner].Id}' already has a body.");
                        }
                        else if (families[owner].Fallback)
                        {
                            reading.Refuse(LineOf(table), $"fallback family '{families[owner].Id}' draws the massing and takes no body.");
                        }
                        else if (body is { House.Variant: HouseVariant.Villa }
                            && families[owner].FrontageMetres is not { Low: >= (long)FamilyBodyBuilder.VillaLeastFrontage })
                        {
                            reading.Refuse(LineOf(table),
                                $"family '{families[owner].Id}' builds a villa, so its 'frontage_metres' must start at {FamilyBodyBuilder.VillaLeastFrontage} or more.");
                        }
                        else if (body is not null)
                        {
                            families[owner] = families[owner] with { Body = body };
                        }

                        break;

                    case ("family.paint", true):
                        PaintScheme? scheme = reading.Scheme();
                        if (lastFamily is not { } painted)
                        {
                            if (!lastFamilyRefused) reading.Refuse(LineOf(table), "[[family.paint]] must follow the [[family]] it paints.");
                        }
                        else if (families[painted].Fallback)
                        {
                            reading.Refuse(LineOf(table), $"fallback family '{families[painted].Id}' draws the massing and takes no paint.");
                        }
                        else if (scheme is not null)
                        {
                            families[painted] = families[painted] with { Paints = [.. families[painted].Paints ?? [], scheme] };
                        }

                        break;

                    default:
                        reading.Refuse(LineOf(table), $"unknown section '{NameOf(table.Name)}'. Expected [preset], [[family]], [family.body] or [[family.paint]].");
                        break;
                }
            }
        }

        if (presetAt is null && errors.Count == 0)
        {
            errors.Add(new AppearanceDiagnostic("(preset)", 0, "no file declares [preset]."));
        }

        CheckFamilies(families, errors);
        return errors.Count > 0 || preset is not { } found
            ? new StylePresetResult(null, errors)
            : new StylePresetResult(new StylePreset(found.Name, found.EraDays, found.Attached, [.. families], found.Runs, found.Terraces, found.ApartmentRuns, found.ShopRuns, found.WorkshopRuns), errors);
    }

    /// <summary>
    /// Warnings for one Ruleset: kinds the Ruleset declares that have no fallback family, and kinds a
    /// family names that the Ruleset does not declare. A preset serves many Rulesets, so neither refuses.
    /// </summary>
    public static IReadOnlyList<AppearanceDiagnostic> CheckAgainst(StylePreset preset, IReadOnlyCollection<string> kinds)
    {
        ArgumentNullException.ThrowIfNull(preset);
        ArgumentNullException.ThrowIfNull(kinds);
        var warnings = new List<AppearanceDiagnostic>();
        foreach (string kind in kinds)
        {
            if (!Array.Exists(preset.Families, f => f.Fallback && Array.IndexOf(f.Kinds, kind) >= 0))
            {
                warnings.Add(new AppearanceDiagnostic("(ruleset)", 0, $"kind '{kind}' has no fallback family."));
            }
        }

        foreach (AppearanceFamily family in preset.Families)
        {
            string[] absent = [.. family.Kinds.Where(k => !kinds.Contains(k))];
            if (absent.Length == 0) continue;
            warnings.Add(new AppearanceDiagnostic(family.File, family.Line,
                $"family '{family.Id}' names kinds this Ruleset does not declare: {string.Join(", ", absent.Select(k => $"'{k}'"))}."));
        }

        return warnings;
    }

    private static void CheckFamilies(List<AppearanceFamily> families, List<AppearanceDiagnostic> errors)
    {
        var ids = new Dictionary<string, AppearanceFamily>(StringComparer.Ordinal);
        var fallbacks = new Dictionary<string, AppearanceFamily>(StringComparer.Ordinal);
        foreach (AppearanceFamily family in families)
        {
            if (!ids.TryAdd(family.Id, family))
            {
                AppearanceFamily first = ids[family.Id];
                errors.Add(new AppearanceDiagnostic(family.File, family.Line,
                    $"family id '{family.Id}' is already declared at {first.File}:{first.Line}."));
            }

            if (family.Paints is not null && family.Body is null)
            {
                errors.Add(new AppearanceDiagnostic(family.File, family.Line,
                    $"family '{family.Id}' has paint but no [family.body] to paint."));
            }

            if (!family.Fallback) continue;
            foreach (string kind in family.Kinds)
            {
                if (!fallbacks.TryAdd(kind, family))
                {
                    AppearanceFamily first = fallbacks[kind];
                    errors.Add(new AppearanceDiagnostic(family.File, family.Line,
                        $"kind '{kind}' already has fallback '{first.Id}' at {first.File}:{first.Line}."));
                }
            }
        }
    }

    private static int LineOf(SyntaxNode node) => node.Span.Start.Line + 1;

    private static string NameOf(KeySyntax? key) =>
        key is null ? string.Empty : string.Join('.', new[] { key.Key }.Concat(key.DotKeys.Select(d => d.Key)).Select(PartOf));

    private static string PartOf(BareKeyOrStringValueSyntax? part) => part switch
    {
        BareKeySyntax bare => bare.Key?.Text ?? string.Empty,
        StringValueSyntax quoted => quoted.Value ?? string.Empty,
        _ => string.Empty,
    };

    private sealed class Reading(string file, TableSyntaxBase table, List<AppearanceDiagnostic> errors)
    {
        private readonly Dictionary<string, KeyValueSyntax> _keys = new(StringComparer.Ordinal);

        public void Refuse(int line, string message) => errors.Add(new AppearanceDiagnostic(file, line, message));

        public (string, int, BlockPattern[], HouseRuns?, Bounds?, Bounds?, Bounds?, Bounds?)? Preset()
        {
            int before = errors.Count;
            Collect(["name", "era_days", "attached", "runs", "run_houses", "terrace_houses", "apartment_lots", "shop_lots", "workshop_lots"]);
            string? name = Text("name", required: true);
            long? era = Integer("era_days", required: true, least: 1);
            BlockPattern[] attached = Names("attached", PatternNames) ?? [];
            HouseRuns? runs = Runs();
            Bounds? terraces = Range("terrace_houses");
            if (terraces is { Low: < 1 }) Refuse(LineOf(_keys["terrace_houses"]), "'terrace_houses' must be [low, high], with low at least 1.");
            if (terraces is not null && attached.Length == 0) Refuse(LineOf(_keys["terrace_houses"]), "'terrace_houses' needs 'attached' patterns to split into terraces.");
            Bounds? apartments = Range("apartment_lots");
            if (apartments is { Low: < 1 }) Refuse(LineOf(_keys["apartment_lots"]), "'apartment_lots' must be [low, high], with low at least 1.");
            Bounds? shops = Range("shop_lots");
            if (shops is { Low: < 1 }) Refuse(LineOf(_keys["shop_lots"]), "'shop_lots' must be [low, high], with low at least 1.");
            Bounds? workshops = Range("workshop_lots");
            if (workshops is { Low: < 1 }) Refuse(LineOf(_keys["workshop_lots"]), "'workshop_lots' must be [low, high], with low at least 1.");
            return errors.Count > before || name is null || era is null ? null : (name, (int)era, attached, runs, terraces, apartments, shops, workshops);
        }

        private HouseRuns? Runs()
        {
            Bounds? houses = Range("run_houses");
            switch (Value("runs", required: false))
            {
                case null:
                    if (houses is not null) Refuse(LineOf(_keys["run_houses"]), "'run_houses' needs 'runs'.");
                    return null;
                case InlineTableSyntax table:
                    var weights = new Dictionary<string, long>(StringComparer.Ordinal);
                    foreach (KeyValueSyntax pair in table.Items.Select(i => i.KeyValue).OfType<KeyValueSyntax>())
                    {
                        string share = NameOf(pair.Key);
                        if (share is not ("alone" or "matched" or "copies"))
                        {
                            Refuse(LineOf(pair), $"'runs.{share}' is not a run. Expected alone, matched or copies.");
                        }
                        else if (pair.Value is IntegerValueSyntax { Value: >= 0 } weight)
                        {
                            weights[share] = weight.Value;
                        }
                        else
                        {
                            Refuse(LineOf(pair), $"'runs.{share}' must be a whole number of at least 0.");
                        }
                    }

                    long alone = weights.GetValueOrDefault("alone");
                    long matched = weights.GetValueOrDefault("matched");
                    long copies = weights.GetValueOrDefault("copies");
                    if (alone + matched + copies <= 0)
                    {
                        Refuse(LineOf(_keys["runs"]), "'runs' must give at least one run a weight above 0.");
                        return null;
                    }

                    if (houses is not { Low: >= 1 } span)
                    {
                        Refuse(LineOf(_keys["runs"]), "'runs' needs 'run_houses' = [low, high], with low at least 1.");
                        return null;
                    }

                    return new HouseRuns((int)alone, (int)matched, (int)copies, span);
                default:
                    Refuse(LineOf(_keys["runs"]), "'runs' must be an inline table of alone, matched and copies weights.");
                    return null;
            }
        }

        public AppearanceFamily? Family()
        {
            int before = errors.Count;
            Collect(["id", "kinds", "weight", "fallback", "model", .. Conditions]);
            string? id = Text("id", required: true);
            string[]? kinds = Strings("kinds", required: true);
            long weight = Integer("weight", required: false, least: 1) ?? 1;
            bool fallback = Boolean("fallback") ?? false;
            string? model = Text("model", required: false);
            Bounds? storeys = Range("storeys");
            Bounds? frontage = Range("frontage_metres");
            Bounds? depth = Range("depth_metres");
            Bounds? raised = Range("raised_day");
            BlockPattern[]? patterns = Names("patterns", PatternNames);
            ushort[]? zones = Names("zones", ZoneNames);
            Bounds? units = Range("units");
            bool? anchored = Boolean("anchored");
            ParkingForm[]? parking = Names("parking", ParkingNames);
            bool? corner = Boolean("corner");

            if (fallback)
            {
                foreach (string condition in Conditions.Where(_keys.ContainsKey))
                {
                    Refuse(LineOf(_keys[condition]), $"a fallback family admits every Building of its kinds, so it takes no '{condition}'.");
                }
            }

            if (kinds is not null)
            {
                foreach (IGrouping<string, string> repeat in kinds.GroupBy(k => k, StringComparer.Ordinal).Where(g => g.Count() > 1))
                {
                    Refuse(LineOf(_keys["kinds"]), $"kind '{repeat.Key}' is listed twice.");
                }
            }

            if (errors.Count > before || id is null || kinds is null) return null;
            return new AppearanceFamily(id, file, LineOf(table), kinds, (int)weight, fallback, model,
                storeys, frontage, depth, raised, patterns, (ushort)(zones?.Aggregate(0, (a, z) => a | z) ?? 0),
                Units: units, Anchored: anchored, Parking: parking, Corner: corner);
        }

        public FamilyBody? Body()
        {
            int before = errors.Count;
            Collect(["library", "shape", "variant", "tile_metres", "materials", "bay_metres", "parapet_metres", "pilasters", "plant", "roof_hatch", "vents", "openings", "gable_degrees", "chimney", "steps", "party_line", "shopfront", "panels", "rooflights", "receiving_canopy", "terrace", "apartment", "workshop", "pitch_degrees", "wide", .. MidriseBodyKeys, .. Rows]);
            string? library = Text("library", required: true);
            Dictionary<string, (float, float)> tiles = Tiles("tile_metres");
            Dictionary<string, string> materials = Materials("materials");
            string? authoredShape = Text("shape", required: false);
            string shape = authoredShape ?? (_keys.ContainsKey("shape") ? string.Empty : "block");
            bool towerShape = shape == "tower";
            bool midriseShape = shape == "midrise";
            bool blockShape = shape == "block";
            bool houseShape = shape == "house";
            bool shopShape = shape == "shop";
            bool workshopShape = shape == "workshop";
            if (!towerShape && !midriseShape && !blockShape && !houseShape && !shopShape && !workshopShape && authoredShape is not null)
            {
                Refuse(LineOf(_keys["shape"]), "'shape' must be \"block\", \"house\", \"shop\", \"workshop\", \"midrise\" or \"tower\".");
            }

            TowerBody? tower = null;
            MidriseBody? midrise = null;
            HouseBody? house = null;
            ShopBody? shop = null;
            WorkshopBody? workshop = null;
            if (towerShape)
            {
                string? variant = Text("variant", required: true);
                TowerVariant? parsed = variant switch
                {
                    "point" => TowerVariant.Point,
                    "stepped-point" => TowerVariant.SteppedPoint,
                    "l" => TowerVariant.L,
                    "h" => TowerVariant.H,
                    _ => null,
                };
                if (variant is not null && parsed is null)
                {
                    Refuse(LineOf(_keys["variant"]), "'variant' must be \"point\", \"stepped-point\", \"l\" or \"h\".");
                }
                else if (parsed is { } found)
                {
                    tower = new TowerBody(found);
                }
            }
            else if (midriseShape)
            {
                string? variant = Text("variant", required: true);
                MidriseVariant? parsed = variant switch
                {
                    "mansion" => MidriseVariant.Mansion,
                    "panel-slab" => MidriseVariant.PanelSlab,
                    _ => null,
                };
                if (variant is not null && parsed is null)
                {
                    Refuse(LineOf(_keys["variant"]), "'variant' must be \"mansion\" or \"panel-slab\".");
                }
                else if (parsed is { } found)
                {
                    midrise = Midrise(found);
                }
            }
            else if (houseShape)
            {
                string? variant = Text("variant", required: true);
                HouseVariant? parsed = variant switch
                {
                    "terrace" => HouseVariant.Terrace,
                    "front-gables" => HouseVariant.FrontGables,
                    "villa" => HouseVariant.Villa,
                    "hipped" => HouseVariant.Hipped,
                    "half-hip" => HouseVariant.HalfHip,
                    "cross-gable" => HouseVariant.CrossGable,
                    "saltbox" => HouseVariant.Saltbox,
                    "gambrel" => HouseVariant.Gambrel,
                    "l-plan" => HouseVariant.LPlan,
                    "rear-outshot" => HouseVariant.RearOutshot,
                    "side-wing" => HouseVariant.SideWing,
                    "staggered-gables" => HouseVariant.StaggeredGables,
                    "flat-setback" => HouseVariant.FlatSetback,
                    "sawtooth" => HouseVariant.Sawtooth,
                    "pavilions" => HouseVariant.Pavilions,
                    "span" => HouseVariant.Span,
                    "butterfly" => HouseVariant.Butterfly,
                    "monopitch" => HouseVariant.Monopitch,
                    "low-hip" => HouseVariant.LowHip,
                    "deep-flat" => HouseVariant.DeepFlat,
                    "deep-gables" => HouseVariant.DeepGables,
                    "deep-sawtooth" => HouseVariant.DeepSawtooth,
                    "deep-span" => HouseVariant.DeepSpan,
                    _ => null,
                };
                bool? wide = Boolean("wide");
                if (variant is not null && parsed is null)
                {
                    Refuse(LineOf(_keys["variant"]), "'variant' must be \"terrace\", \"front-gables\", \"villa\", \"hipped\", \"half-hip\", \"cross-gable\", \"saltbox\", \"gambrel\", \"l-plan\", \"rear-outshot\", \"side-wing\", \"staggered-gables\", \"flat-setback\", \"sawtooth\", \"pavilions\", \"span\", \"butterfly\", \"monopitch\", \"low-hip\", \"deep-flat\", \"deep-gables\", \"deep-sawtooth\" or \"deep-span\".");
                }
                else if (parsed is { } found)
                {
                    bool old = found is HouseVariant.Terrace or HouseVariant.FrontGables or HouseVariant.Villa;
                    if (wide is not null && !old)
                    {
                        Refuse(LineOf(_keys["wide"]), FamilyBodyBuilder.IsWideForm(found)
                            ? "a wide house form is always wide: drop 'wide'."
                            : "'wide' is only valid on a \"terrace\", \"front-gables\" or \"villa\" house.");
                    }

                    house = new HouseBody(found, FamilyBodyBuilder.IsWideForm(found) || (old && wide == true));
                }
            }
            else if (shopShape)
            {
                shop = Shop();
            }
            else if (workshopShape)
            {
                workshop = Workshop();
            }
            else if (_keys.TryGetValue("variant", out KeyValueSyntax? strayVariant))
            {
                Refuse(LineOf(strayVariant), "'variant' is only valid when 'shape' is \"tower\", \"midrise\", \"house\", \"shop\" or \"workshop\".");
            }

            if (!houseShape && _keys.TryGetValue("wide", out KeyValueSyntax? strayWide))
            {
                Refuse(LineOf(strayWide), "'wide' is only valid for a house body.");
            }

            if (!midriseShape)
            {
                foreach (string key in MidriseBodyKeys)
                {
                    if (_keys.TryGetValue(key, out KeyValueSyntax? refused))
                    {
                        Refuse(LineOf(refused), $"'{key}' is only valid for a mid-rise body.");
                    }
                }
            }

            if (towerShape || midriseShape || houseShape || shopShape || workshopShape)
            {
                foreach (string key in LowRiseBodyKeys)
                {
                    if ((shopShape || workshopShape) && key == "pitch_degrees") continue;
                    if (_keys.TryGetValue(key, out KeyValueSyntax? refused))
                    {
                        Refuse(LineOf(refused), $"'{key}' is only valid for a block body.");
                    }
                }
            }

            float bay = 0f;
            float parapet = 0f;
            bool pilasters = false;
            long plant = 0;
            bool hatch = false;
            long vents = 0;
            Dictionary<BayKind, OpeningSize> openings = [];
            float gable = 0f;
            bool chimney = false;
            bool steps = false;
            bool partyLine = false;
            bool shopfront = false;
            bool panels = false;
            bool rooflights = false;
            bool receiving = false;
            BayRow[] rows = [BayRow.Blank, BayRow.Blank, BayRow.Blank, BayRow.Blank, BayRow.Blank, BayRow.Blank];
            TerraceBody? terrace = null;
            ApartmentBody? apartment = null;
            if (blockShape)
            {
                bay = Metres("bay_metres", required: true, least: 1f) ?? 0f;
                parapet = Metres("parapet_metres", required: false, least: 0f) ?? 0f;
                pilasters = Boolean("pilasters") ?? false;
                plant = Integer("plant", required: false, least: 0) ?? 0;
                hatch = Boolean("roof_hatch") ?? false;
                vents = Integer("vents", required: false, least: 0) ?? 0;
                openings = Openings("openings");
                gable = Metres("gable_degrees", required: false, least: 18f, unit: "degrees") ?? 0f;
                chimney = Boolean("chimney") ?? false;
                steps = Boolean("steps") ?? false;
                partyLine = Boolean("party_line") ?? false;
                shopfront = Boolean("shopfront") ?? false;
                panels = Boolean("panels") ?? false;
                rooflights = Boolean("rooflights") ?? false;
                receiving = Boolean("receiving_canopy") ?? false;
                if (gable > 0f && parapet > 0f) Refuse(LineOf(_keys["gable_degrees"]), "a gable roof takes no parapet: give 'gable_degrees' or 'parapet_metres', not both.");
                terrace = Terrace();
                apartment = terrace is null ? Apartment() : null;
                if (terrace is not null && _keys.TryGetValue("apartment", out KeyValueSyntax? both)) Refuse(LineOf(both), "a body takes 'terrace' or 'apartment', not both.");
                workshop = PanelShed();
                if (chimney && gable == 0f && terrace is null) Refuse(LineOf(_keys["chimney"]), "a chimney stands on a gable roof: 'chimney' needs 'gable_degrees'.");

                // The far box takes its cap from these, so a terrace or apartment form gives them its own roof's.
                if ((terrace?.PitchDegrees ?? apartment?.PitchDegrees) is { } pitch) gable = (pitch.Low + pitch.High) / 2f;
                else if (terrace?.Form == TerraceForm.Parapet) parapet = FamilyBodyBuilder.TerraceParapetMetres;
                rows = [.. Rows.Select(Row)];
            }

            return errors.Count > before || library is null || !blockShape && tower is null && midrise is null && house is null && shop is null && workshop is null
                ? null
                : new FamilyBody(library, tiles, materials, bay, parapet, pilasters, (int)plant,
                    new WallRule(rows[0], rows[1]), new WallRule(rows[2], rows[3]), new WallRule(rows[4], rows[5]),
                    hatch, (int)vents, openings, gable, chimney, steps, partyLine, shopfront, panels, rooflights, receiving, tower, midrise, house, terrace, apartment, shop, workshop);
        }

        private TerraceBody? Terrace()
        {
            string? name = Text("terrace", required: false);
            if (name is null)
            {
                if (_keys.TryGetValue("pitch_degrees", out KeyValueSyntax? alone) && !_keys.ContainsKey("apartment"))
                {
                    Refuse(LineOf(alone), "'pitch_degrees' needs 'terrace' or 'apartment'.");
                }

                return null;
            }

            Bounds? pitch = Range("pitch_degrees");
            TerraceForm? form = name switch
            {
                "side-gable" => TerraceForm.SideGable,
                "parapet" => TerraceForm.Parapet,
                "mansard" => TerraceForm.Mansard,
                "front-gable" => TerraceForm.FrontGable,
                "paired-gable" => TerraceForm.PairedGable,
                "bay-front" => TerraceForm.BayFront,
                _ => null,
            };
            if (form is not { } found)
            {
                Refuse(LineOf(_keys["terrace"]), "'terrace' must be \"side-gable\", \"parapet\", \"mansard\", \"front-gable\", \"paired-gable\" or \"bay-front\".");
                return null;
            }

            foreach (string own in (ReadOnlySpan<string>)["gable_degrees", "parapet_metres"])
            {
                if (_keys.TryGetValue(own, out KeyValueSyntax? refused)) Refuse(LineOf(refused), $"a terrace form draws its own roof, so it takes no '{own}'.");
            }

            bool pitched = found is not (TerraceForm.Parapet or TerraceForm.Mansard);
            if (pitched && pitch is not { Low: >= 18, High: <= 45 })
            {
                Refuse(LineOf(_keys["terrace"]), "a pitched terrace form needs 'pitch_degrees' = [low, high] within 18 to 45 degrees.");
                return null;
            }

            if (!pitched && pitch is not null)
            {
                Refuse(LineOf(_keys["pitch_degrees"]), $"a {name} roof has no pitch, so it takes no 'pitch_degrees'.");
                return null;
            }

            return new TerraceBody(found, pitch);
        }

        private ShopBody? Shop()
        {
            string? name = Text("variant", required: true);
            ShopForm? form = name switch
            {
                "victorian-gable" => ShopForm.VictorianGable,
                "gabled-oriel" => ShopForm.GabledOriel,
                "turret-corner" => ShopForm.TurretCorner,
                "main-street" => ShopForm.MainStreet,
                "mansard" => ShopForm.Mansard,
                "interwar-curve" => ShopForm.InterwarCurve,
                "sixties-canopy" => ShopForm.SixtiesCanopy,
                "setback-modern" => ShopForm.SetbackModern,
                "pantile-hipped" => ShopForm.PantileHipped,
                _ => null,
            };
            if (name is null) return null;
            if (form is not { } found)
            {
                Refuse(LineOf(_keys["variant"]), "a shop 'variant' must be \"victorian-gable\", \"gabled-oriel\", \"turret-corner\", \"main-street\", \"mansard\", \"interwar-curve\", \"sixties-canopy\", \"setback-modern\" or \"pantile-hipped\".");
                return null;
            }

            bool pitched = FamilyBodyBuilder.ShopPitched(found);
            Bounds? pitch = Range("pitch_degrees");
            if (pitched && pitch is not { Low: >= 18, High: <= 45 })
            {
                Refuse(LineOf(_keys["variant"]), "a pitched shop form needs 'pitch_degrees' = [low, high] within 18 to 45 degrees.");
                return null;
            }

            if (!pitched && pitch is not null)
            {
                Refuse(LineOf(_keys["pitch_degrees"]), $"a shop {name} has no pitch, so it takes no 'pitch_degrees'.");
                return null;
            }

            return new ShopBody(found, pitch);
        }

        /// <summary>A block body tagged as the panel shed among the workshop forms.</summary>
        private WorkshopBody? PanelShed()
        {
            string? name = Text("workshop", required: false);
            if (name is null) return null;
            if (name != "panel-shed")
            {
                Refuse(LineOf(_keys["workshop"]), "a block body's 'workshop' must be \"panel-shed\"; the other workshop forms are 'shape = \"workshop\"'.");
                return null;
            }

            if (_keys.ContainsKey("terrace") || _keys.ContainsKey("apartment"))
            {
                Refuse(LineOf(_keys["workshop"]), "a body takes one of 'terrace', 'apartment' and 'workshop'.");
                return null;
            }

            return new WorkshopBody(WorkshopForm.PanelShed, null);
        }

        private WorkshopBody? Workshop()
        {
            string? name = Text("variant", required: true);
            WorkshopForm? form = name switch
            {
                "builders-yard" => WorkshopForm.BuildersYard,
                "monitor" => WorkshopForm.Monitor,
                "interwar-garage" => WorkshopForm.InterwarGarage,
                "parapet-roller" => WorkshopForm.ParapetRoller,
                "portal-shed" => WorkshopForm.PortalShed,
                _ => null,
            };
            if (name is null) return null;
            if (form is not { } found)
            {
                Refuse(LineOf(_keys["variant"]), "a workshop 'variant' must be \"builders-yard\", \"monitor\", \"interwar-garage\", \"parapet-roller\" or \"portal-shed\".");
                return null;
            }

            bool pitched = FamilyBodyBuilder.WorkshopPitched(found);
            Bounds? pitch = Range("pitch_degrees");
            if (pitched && pitch is not { Low: >= 5, High: <= 45 })
            {
                Refuse(LineOf(_keys["variant"]), "a pitched workshop form needs 'pitch_degrees' = [low, high] within 5 to 45 degrees.");
                return null;
            }

            if (!pitched && pitch is not null)
            {
                Refuse(LineOf(_keys["pitch_degrees"]), $"a workshop {name} has no pitch, so it takes no 'pitch_degrees'.");
                return null;
            }

            return new WorkshopBody(found, pitch);
        }

        private ApartmentBody? Apartment()
        {
            string? name = Text("apartment", required: false);
            if (name is null) return null;
            ApartmentForm? form = name switch
            {
                "parapet" => ApartmentForm.Parapet,
                "hipped" => ApartmentForm.Hipped,
                "side-gable" => ApartmentForm.SideGable,
                "mansard" => ApartmentForm.Mansard,
                "centre-gable" => ApartmentForm.CentreGable,
                "bay-front" => ApartmentForm.BayFront,
                "forecourt" => ApartmentForm.Forecourt,
                "setback" => ApartmentForm.Setback,
                _ => null,
            };
            if (form is not { } found)
            {
                Refuse(LineOf(_keys["apartment"]), "'apartment' must be \"parapet\", \"hipped\", \"side-gable\", \"mansard\", \"centre-gable\", \"bay-front\", \"forecourt\" or \"setback\".");
                return null;
            }

            bool pitched = found is ApartmentForm.Hipped or ApartmentForm.SideGable or ApartmentForm.CentreGable;
            string[] own = pitched || found == ApartmentForm.Mansard ? ["gable_degrees", "parapet_metres"] : ["gable_degrees"];
            foreach (string key in own)
            {
                if (_keys.TryGetValue(key, out KeyValueSyntax? refused)) Refuse(LineOf(refused), $"an apartment {name} draws its own roof, so it takes no '{key}'.");
            }

            Bounds? pitch = Range("pitch_degrees");
            if (pitched && pitch is not { Low: >= 18, High: <= 45 })
            {
                Refuse(LineOf(_keys["apartment"]), "a pitched apartment form needs 'pitch_degrees' = [low, high] within 18 to 45 degrees.");
                return null;
            }

            if (!pitched && pitch is not null)
            {
                Refuse(LineOf(_keys["pitch_degrees"]), $"an apartment {name} has no pitch, so it takes no 'pitch_degrees'.");
                return null;
            }

            return new ApartmentBody(found, pitch);
        }

        private MidriseBody Midrise(MidriseVariant variant)
        {
            float? module = Metres("module_metres", required: false, least: 2.5f);
            string? openings = Text("street_openings", required: false);
            StreetOpenings street = openings switch
            {
                "balconies" => StreetOpenings.Balconies,
                "windows" => StreetOpenings.Windows,
                _ => StreetOpenings.Loggias,
            };
            if (openings is not null and not ("loggias" or "balconies" or "windows"))
            {
                Refuse(LineOf(_keys["street_openings"]), "'street_openings' must be \"loggias\", \"balconies\" or \"windows\".");
            }

            (string Key, MidriseVariant Variant)[] owned = [("attic", MidriseVariant.Mansion), ("galleries", MidriseVariant.PanelSlab)];
            foreach ((string key, MidriseVariant owner) in owned)
            {
                if (owner != variant && _keys.TryGetValue(key, out KeyValueSyntax? refused))
                {
                    Refuse(LineOf(refused), $"'{key}' is only valid for a {(owner == MidriseVariant.Mansion ? "mansion" : "panel-slab")} body.");
                }
            }

            return new MidriseBody(variant, module, street, Boolean("shops") ?? false,
                Boolean("attic") ?? true, Boolean("galleries") ?? true);
        }

        public PaintScheme? Scheme()
        {
            int before = errors.Count;
            Collect(["weight", .. FamilyBodyBuilder.PartNames]);
            long weight = Integer("weight", required: false, least: 1) ?? 1;
            var parts = new Dictionary<string, Paint>(StringComparer.Ordinal);
            foreach (string part in FamilyBodyBuilder.PartNames)
            {
                string? code = Text(part, required: false);
                if (code is null) continue;
                if (code.Length == 6 && uint.TryParse(code, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out uint rgb))
                {
                    parts[part] = new Paint((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb);
                }
                else
                {
                    Refuse(LineOf(_keys[part]), $"'{part}' must be an sRGB colour of six hex digits, such as \"b9ad97\".");
                }
            }

            if (parts.Count == 0 && errors.Count == before) Refuse(LineOf(table), "a paint scheme must colour at least one part.");
            return errors.Count > before ? null : new PaintScheme(parts, (int)weight);
        }

        private Dictionary<BayKind, OpeningSize> Openings(string key)
        {
            var sizes = new Dictionary<BayKind, OpeningSize>();
            switch (Value(key, required: false))
            {
                case null:
                    return sizes;
                case InlineTableSyntax table:
                    foreach (KeyValueSyntax pair in table.Items.Select(i => i.KeyValue).OfType<KeyValueSyntax>())
                    {
                        string name = NameOf(pair.Key);
                        float?[] numbers = pair.Value is ArraySyntax { Items.ChildrenCount: 2 or 3 } array
                            ? [.. array.Items.Select(i => Number(i.Value))]
                            : [];
                        if (!SizedOpenings.TryGetValue(name, out BayKind kind))
                        {
                            Refuse(LineOf(pair), $"'{key}.{name}' names no sized opening. Expected one of: {string.Join(", ", SizedOpenings.Keys)}.");
                        }
                        else if (numbers.Length > 0 && numbers[0] > 0f && numbers[1] > 0f && (numbers.Length == 2 || numbers[2] >= 0f))
                        {
                            sizes[kind] = new OpeningSize(numbers[0]!.Value, numbers[1]!.Value, numbers.Length == 3 ? numbers[2] : null);
                        }
                        else
                        {
                            Refuse(LineOf(pair), $"'{key}.{name}' must be [width, height] or [width, height, sill] in metres: positive sizes and a sill not below the floor.");
                        }
                    }

                    return sizes;
                default:
                    Refuse(LineOf(_keys[key]), $"'{key}' must be an inline table of opening = [width, height] or [width, height, sill].");
                    return sizes;
            }
        }

        private BayRow Row(string key)
        {
            string[]? tokens = Strings(key, required: false);
            if (tokens is null) return BayRow.Blank;
            var laid = new List<BayKind[]>();
            var fills = new List<bool>();
            foreach (string token in tokens)
            {
                bool fill = token.EndsWith('*');
                string[] names = (fill ? token[..^1] : token).Split('+');
                var kinds = new List<BayKind>();
                foreach (string name in names)
                {
                    if (BayNames.TryGetValue(name, out BayKind kind)) kinds.Add(kind);
                }

                if (kinds.Count != names.Length)
                {
                    Refuse(LineOf(_keys[key]), $"'{token}' is not a bay. Expected one of: {string.Join(", ", BayNames.Keys)}, several joined by '+' to alternate, each token optionally ending in '*' to fill.");
                    continue;
                }

                laid.Add([.. kinds]);
                fills.Add(fill);
            }

            return new BayRow([.. laid], [.. fills]);
        }

        private float? Metres(string key, bool required, float least, string unit = "metres")
        {
            ValueSyntax? value = Value(key, required);
            float? metres = value switch
            {
                null => null,
                IntegerValueSyntax whole => whole.Value,
                FloatValueSyntax real => (float)real.Value,
                _ => float.NaN,
            };
            if (metres is { } m && !(m >= least))
            {
                Refuse(LineOf(_keys[key]), $"'{key}' must be a number of at least {least} {unit}.");
                return null;
            }

            return metres;
        }

        private Dictionary<string, (float, float)> Tiles(string key)
        {
            var tiles = new Dictionary<string, (float, float)>(StringComparer.Ordinal);
            switch (Value(key, required: false))
            {
                case null:
                    return tiles;
                case InlineTableSyntax table:
                    foreach (KeyValueSyntax pair in table.Items.Select(i => i.KeyValue).OfType<KeyValueSyntax>())
                    {
                        string part = NameOf(pair.Key);
                        if (Array.IndexOf(FamilyBodyBuilder.PartNames, part) < 0)
                        {
                            Refuse(LineOf(pair), $"'{key}.{part}' names no body part. Expected one of: {string.Join(", ", FamilyBodyBuilder.PartNames)}.");
                        }
                        else if (pair.Value is ArraySyntax { Items.ChildrenCount: 2 } array
                            && Number(array.Items.GetChild(0)?.Value) is { } along and > 0f
                            && Number(array.Items.GetChild(1)?.Value) is { } up and > 0f)
                        {
                            tiles[part] = (along, up);
                        }
                        else
                        {
                            Refuse(LineOf(pair), $"'{key}.{part}' must be [along, up]: two positive numbers of metres.");
                        }
                    }

                    return tiles;
                default:
                    Refuse(LineOf(_keys[key]), $"'{key}' must be an inline table of part = [along, up].");
                    return tiles;
            }
        }

        private Dictionary<string, string> Materials(string key)
        {
            var materials = new Dictionary<string, string>(StringComparer.Ordinal);
            switch (Value(key, required: false))
            {
                case null:
                    return materials;
                case InlineTableSyntax table:
                    foreach (KeyValueSyntax pair in table.Items.Select(i => i.KeyValue).OfType<KeyValueSyntax>())
                    {
                        string part = NameOf(pair.Key);
                        if (Array.IndexOf(FamilyBodyBuilder.PartNames, part) < 0)
                        {
                            Refuse(LineOf(pair), $"'{key}.{part}' names no body part. Expected one of: {string.Join(", ", FamilyBodyBuilder.PartNames)}.");
                        }
                        else if (pair.Value is StringValueSyntax { Value: { Length: > 0 } texture })
                        {
                            materials[part] = texture;
                        }
                        else
                        {
                            Refuse(LineOf(pair), $"'{key}.{part}' must name a texture library entry, such as \"bricks-088\".");
                        }
                    }

                    return materials;
                default:
                    Refuse(LineOf(_keys[key]), $"'{key}' must be an inline table of part = \"texture\".");
                    return materials;
            }
        }

        private static float? Number(ValueSyntax? value) => value switch
        {
            IntegerValueSyntax whole => whole.Value,
            FloatValueSyntax real => (float)real.Value,
            _ => null,
        };

        private void Collect(string[] known)
        {
            foreach (SyntaxNode item in table.Items)
            {
                if (item is not KeyValueSyntax pair) continue;
                string key = NameOf(pair.Key);
                if (Array.IndexOf(known, key) < 0)
                {
                    Refuse(LineOf(pair), $"unknown key '{key}'. Expected one of: {string.Join(", ", known)}.");
                }
                else if (!_keys.TryAdd(key, pair))
                {
                    Refuse(LineOf(pair), $"'{key}' is given twice.");
                }
            }
        }

        private ValueSyntax? Value(string key, bool required)
        {
            if (_keys.TryGetValue(key, out KeyValueSyntax? pair)) return pair.Value;
            if (required) Refuse(LineOf(table), $"'{key}' is required.");
            return null;
        }

        private string? Text(string key, bool required)
        {
            switch (Value(key, required))
            {
                case null: return null;
                case StringValueSyntax { Value: { Length: > 0 } text }: return text;
                default: Refuse(LineOf(_keys[key]), $"'{key}' must be a non-empty string."); return null;
            }
        }

        private bool? Boolean(string key)
        {
            switch (Value(key, required: false))
            {
                case null: return null;
                case BooleanValueSyntax flag: return flag.Value;
                default: Refuse(LineOf(_keys[key]), $"'{key}' must be true or false."); return null;
            }
        }

        private long? Integer(string key, bool required, long least)
        {
            switch (Value(key, required))
            {
                case null: return null;
                case IntegerValueSyntax number when number.Value >= least: return number.Value;
                default: Refuse(LineOf(_keys[key]), $"'{key}' must be a whole number of at least {least}."); return null;
            }
        }

        private string[]? Strings(string key, bool required)
        {
            switch (Value(key, required))
            {
                case null: return null;
                case ArraySyntax array when array.Items.ChildrenCount > 0
                    && array.Items.All(i => i.Value is StringValueSyntax { Value.Length: > 0 }):
                    return [.. array.Items.Select(i => ((StringValueSyntax)i.Value!).Value!)];
                default: Refuse(LineOf(_keys[key]), $"'{key}' must be a non-empty array of non-empty strings."); return null;
            }
        }

        private T[]? Names<T>(string key, Dictionary<string, T> allowed)
        {
            string[]? names = Strings(key, required: false);
            if (names is null) return null;
            var values = new List<T>();
            foreach (string name in names)
            {
                if (allowed.TryGetValue(name, out T? value)) values.Add(value);
                else Refuse(LineOf(_keys[key]), $"'{name}' is not a {key} value. Expected one of: {string.Join(", ", allowed.Keys)}.");
            }

            return [.. values];
        }

        private Bounds? Range(string key)
        {
            switch (Value(key, required: false))
            {
                case null: return null;
                case ArraySyntax { Items.ChildrenCount: 2 } array
                    when array.Items.GetChild(0)?.Value is IntegerValueSyntax low
                        && array.Items.GetChild(1)?.Value is IntegerValueSyntax high
                        && low.Value >= 0 && low.Value <= high.Value:
                    return new Bounds(low.Value, high.Value);
                default:
                    Refuse(LineOf(_keys[key]), $"'{key}' must be [low, high]: two whole numbers, not negative, low no greater than high.");
                    return null;
            }
        }
    }
}
