using System;
using System.Buffers.Binary;
using System.IO;
using System.Linq;
using System.Text.Json;
using Borough.Appearance;

namespace Borough.Tests.Appearance;

public sealed class BodyFadeTests
{
    [Theory]
    [InlineData("far-shader.gdshader")]
    [InlineData("far-grid.gdshader")]
    [InlineData("library-body.gdshader")]
    [InlineData("body-kit.gdshader")]
    public void EveryBodyShaderUsesTheSameInstanceFade(string shader)
    {
        string source = File.ReadAllText(Path.Combine(RepoRoot(), "src", "Borough.Godot", shader));
        Assert.Contains("#include \"body-fade.gdshaderinc\"", source);
        Assert.Contains("fade_in = body_fade_weight(MODEL_MATRIX[3].xyz, MAIN_CAM_INV_VIEW_MATRIX[3].xyz)", source);
        Assert.Contains("POSITION = body_fade_position(VERTEX, MODELVIEW_MATRIX, PROJECTION_MATRIX)", source);
        Assert.Contains("if (body_fade_discards(fade_in, FRAGCOORD.xy))", source);
        Assert.DoesNotContain("uniform float fade_", source);
    }

    [Fact]
    public void DefaultsChooseFilteredShaderWindowsAndFade()
    {
        string shell = Path.Combine(RepoRoot(), "src", "Borough.Godot");
        Assert.Contains("_farWindowName = \"shader\"", File.ReadAllText(Path.Combine(shell, "Main.FarWindows.cs")));
        Assert.Contains("_farFade = true", File.ReadAllText(Path.Combine(shell, "Main.FarFade.cs")));
        Assert.DoesNotContain("uniform bool filtered", File.ReadAllText(Path.Combine(shell, "far-shader.gdshader")));
        string shared = File.ReadAllText(Path.Combine(shell, "body-fade.gdshaderinc"));
        Assert.Contains("fade_near_side ? weight > threshold : weight <= threshold", shared);
        Assert.Contains("fade_near_side ? -1.0 : 2.0", shared);
    }

    [Fact]
    public void AuthoredFadingBodyMaterialsUseOnlyRebuiltProperties()
    {
        string root = RepoRoot();
        StylePresetResult result = StylePresetReader.Read(Path.Combine(root, "appearance", "test-street"));
        Assert.NotNull(result.Preset);
        string[] libraries = result.Preset!.Families
            .Where(family => family.Body is { } body && (body.Midrise is not null || body.Tower is not null))
            .Select(family => family.Body!.Library).Distinct().ToArray();
        Assert.NotEmpty(libraries);
        foreach (string library in libraries)
        {
            byte[] glb = File.ReadAllBytes(Path.Combine(root, "src", "Borough.Godot", "assets", library + ".glb"));
            int bytes = checked((int)BinaryPrimitives.ReadUInt32LittleEndian(glb.AsSpan(12, 4)));
            using JsonDocument document = JsonDocument.Parse(glb.AsMemory(20, bytes));
            foreach (JsonElement material in document.RootElement.GetProperty("materials").EnumerateArray())
            {
                string name = material.GetProperty("name").GetString()!;
                foreach (JsonProperty property in material.EnumerateObject())
                {
                    Assert.True(property.Name is "name" or "pbrMetallicRoughness" or "doubleSided",
                        $"{library} material '{name}' property '{property.Name}' is not carried by body-kit.gdshader.");
                }

                Assert.True(material.TryGetProperty("doubleSided", out JsonElement doubleSided) && doubleSided.GetBoolean(),
                    $"{library} material '{name}' must match body-kit.gdshader's disabled culling.");
                foreach (JsonProperty property in material.GetProperty("pbrMetallicRoughness").EnumerateObject())
                {
                    Assert.True(property.Name is "baseColorFactor" or "metallicFactor" or "roughnessFactor",
                        $"{library} material '{name}' property '{property.Name}' is not carried by body-kit.gdshader.");
                }
            }
        }
    }

    private static string RepoRoot()
    {
        DirectoryInfo? at = new(AppContext.BaseDirectory);
        while (at is not null && !File.Exists(Path.Combine(at.FullName, "CLAUDE.md"))) at = at.Parent;
        Assert.NotNull(at);
        return at!.FullName;
    }
}
