using System.Text.Json;

namespace Borough.Appearance;

/// <summary>
/// The textures a family body can be dressed from, as
/// <c>src/Borough.Godot/assets/city/library/materials.json</c> lists them.
/// </summary>
/// <param name="TileMetres">The width and height one tile of each texture covers, by texture name.</param>
/// <param name="Paintable">
/// The textures made greyscale for a family's paint to colour. Any other texture keeps its own colour.
/// </param>
/// <param name="MeanLuminance">Each texture's colour map mean linear luminance, which paint divides out.</param>
public sealed record TextureLibrary(
    IReadOnlyDictionary<string, (float Along, float Up)> TileMetres,
    IReadOnlySet<string> Paintable,
    IReadOnlyDictionary<string, float> MeanLuminance)
{
    public static TextureLibrary Read(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        var tiles = new Dictionary<string, (float, float)>(StringComparer.Ordinal);
        var paintable = new HashSet<string>(StringComparer.Ordinal);
        var means = new Dictionary<string, float>(StringComparer.Ordinal);
        foreach (JsonProperty texture in document.RootElement.GetProperty("textures").EnumerateObject())
        {
            JsonElement size = texture.Value.GetProperty("tile_metres");
            tiles[texture.Name] = (size[0].GetSingle(), size[1].GetSingle());
            if (texture.Value.TryGetProperty("paint", out JsonElement paint) && paint.GetBoolean()) paintable.Add(texture.Name);
            means[texture.Name] = texture.Value.GetProperty("albedo_linear_mean_luminance").GetSingle();
        }

        return new TextureLibrary(tiles, paintable, means);
    }

    /// <summary>
    /// The preset with each dressed part tiling at its texture's size, unless the body lists that part
    /// in <c>tile_metres</c>. A texture the library lacks refuses the preset.
    /// </summary>
    public StylePresetResult Dress(StylePreset preset)
    {
        ArgumentNullException.ThrowIfNull(preset);
        var errors = new List<AppearanceDiagnostic>();
        AppearanceFamily[] families = [.. preset.Families.Select(family =>
        {
            if (family.Body is not { } body || body.Materials.Count == 0) return family;
            var tiles = new Dictionary<string, (float Along, float Up)>(body.TileMetres, StringComparer.Ordinal);
            foreach (string part in FamilyBodyBuilder.PartNames)
            {
                if (!body.Materials.TryGetValue(part, out string? texture)) continue;
                if (!TileMetres.TryGetValue(texture, out (float, float) size))
                {
                    errors.Add(new AppearanceDiagnostic(family.File, family.Line,
                        $"family '{family.Id}' dresses '{part}' in '{texture}', which the texture library does not hold."));
                }
                else
                {
                    tiles.TryAdd(part, size);
                }
            }

            return family with { Body = body with { TileMetres = tiles } };
        })];

        return errors.Count > 0
            ? new StylePresetResult(null, errors)
            : new StylePresetResult(preset with { Families = families }, errors);
    }
}
