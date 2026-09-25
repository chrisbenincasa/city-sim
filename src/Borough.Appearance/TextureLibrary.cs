using System.Text.Json;

namespace Borough.Appearance;

/// <summary>One texture in the library.</summary>
/// <param name="Along">The width one tile of the texture covers, in metres.</param>
/// <param name="Up">The height one tile of the texture covers, in metres.</param>
/// <param name="MeanLuminance">The colour map's mean linear luminance, which paint divides out.</param>
/// <param name="Paint">A family's paint carries the hue. Otherwise the photograph keeps its colour and takes no paint.</param>
public readonly record struct LibraryTexture(float Along, float Up, float MeanLuminance, bool Paint);

/// <summary>
/// The textures a family body can be dressed from, as <c>assets/city/library/materials.json</c>
/// in the Godot project lists them.
/// </summary>
public sealed record TextureLibrary(IReadOnlyDictionary<string, LibraryTexture> Textures)
{
    public static TextureLibrary Read(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        var textures = new Dictionary<string, LibraryTexture>(StringComparer.Ordinal);
        foreach (JsonProperty texture in document.RootElement.GetProperty("textures").EnumerateObject())
        {
            JsonElement size = texture.Value.GetProperty("tile_metres");
            textures[texture.Name] = new LibraryTexture(size[0].GetSingle(), size[1].GetSingle(),
                texture.Value.GetProperty("albedo_linear_mean_luminance").GetSingle(),
                texture.Value.GetProperty("paint").GetBoolean());
        }

        return new TextureLibrary(textures);
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
                if (!body.Materials.TryGetValue(part, out string? name)) continue;
                if (!Textures.TryGetValue(name, out LibraryTexture texture))
                {
                    errors.Add(new AppearanceDiagnostic(family.File, family.Line,
                        $"family '{family.Id}' dresses '{part}' in '{name}', which the texture library does not hold."));
                }
                else
                {
                    tiles.TryAdd(part, (texture.Along, texture.Up));
                }
            }

            return family with { Body = body with { TileMetres = tiles } };
        })];

        return errors.Count > 0
            ? new StylePresetResult(null, errors)
            : new StylePresetResult(preset with { Families = families }, errors);
    }
}
