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
    };

    private static readonly Dictionary<string, BayKind> SizedOpenings = new()
    {
        ["window"] = BayKind.Window,
        ["door"] = BayKind.Door,
        ["stair"] = BayKind.Stair,
    };

    private static readonly string[] Conditions = ["storeys", "frontage_metres", "depth_metres", "raised_day", "patterns", "zones"];

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
        (string Name, int EraDays, BlockPattern[] Attached)? preset = null;
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
                        else if (body is not null)
                        {
                            families[owner] = families[owner] with { Body = body };
                        }

                        break;

                    default:
                        reading.Refuse(LineOf(table), $"unknown section '{NameOf(table.Name)}'. Expected [preset], [[family]] or [family.body].");
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
            : new StylePresetResult(new StylePreset(found.Name, found.EraDays, found.Attached, [.. families]), errors);
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

        public (string, int, BlockPattern[])? Preset()
        {
            int before = errors.Count;
            Collect(["name", "era_days", "attached"]);
            string? name = Text("name", required: true);
            long? era = Integer("era_days", required: true, least: 1);
            BlockPattern[] attached = Names("attached", PatternNames) ?? [];
            return errors.Count > before || name is null || era is null ? null : (name, (int)era, attached);
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
                storeys, frontage, depth, raised, patterns, (ushort)(zones?.Aggregate(0, (a, z) => a | z) ?? 0));
        }

        public FamilyBody? Body()
        {
            int before = errors.Count;
            Collect(["library", "tile_metres", "bay_metres", "parapet_metres", "pilasters", "plant", "roof_hatch", "vents", "openings", .. Rows]);
            string? library = Text("library", required: true);
            Dictionary<string, (float, float)> tiles = Tiles("tile_metres");
            float bay = Metres("bay_metres", required: true, least: 1f) ?? 0f;
            float parapet = Metres("parapet_metres", required: false, least: 0f) ?? 0f;
            bool pilasters = Boolean("pilasters") ?? false;
            long plant = Integer("plant", required: false, least: 0) ?? 0;
            bool hatch = Boolean("roof_hatch") ?? false;
            long vents = Integer("vents", required: false, least: 0) ?? 0;
            Dictionary<BayKind, OpeningSize> openings = Openings("openings");
            BayRow[] rows = [.. Rows.Select(Row)];
            return errors.Count > before || library is null
                ? null
                : new FamilyBody(library, tiles, bay, parapet, pilasters, (int)plant,
                    new WallRule(rows[0], rows[1]), new WallRule(rows[2], rows[3]), new WallRule(rows[4], rows[5]),
                    hatch, (int)vents, openings);
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
            var kinds = new List<BayKind>();
            var fills = new List<bool>();
            foreach (string token in tokens)
            {
                bool fill = token.EndsWith('*');
                string name = fill ? token[..^1] : token;
                if (!BayNames.TryGetValue(name, out BayKind kind))
                {
                    Refuse(LineOf(_keys[key]), $"'{token}' is not a bay. Expected one of: {string.Join(", ", BayNames.Keys)}, each optionally ending in '*' to fill.");
                    continue;
                }

                kinds.Add(kind);
                fills.Add(fill);
            }

            return new BayRow([.. kinds], [.. fills]);
        }

        private float? Metres(string key, bool required, float least)
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
                Refuse(LineOf(_keys[key]), $"'{key}' must be a number of at least {least} metres.");
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
                        if (pair.Value is ArraySyntax { Items.ChildrenCount: 2 } array
                            && Number(array.Items.GetChild(0)?.Value) is { } along and > 0f
                            && Number(array.Items.GetChild(1)?.Value) is { } up and > 0f)
                        {
                            tiles[NameOf(pair.Key)] = (along, up);
                        }
                        else
                        {
                            Refuse(LineOf(pair), $"'{key}.{NameOf(pair.Key)}' must be [along, up]: two positive numbers of metres.");
                        }
                    }

                    return tiles;
                default:
                    Refuse(LineOf(_keys[key]), $"'{key}' must be an inline table of part = [along, up].");
                    return tiles;
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
