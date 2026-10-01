using System.Numerics;
using System.Text.Json;
using Borough.Appearance;

namespace Borough.Tests.Appearance;

public sealed class MidriseBodyTests
{
    [Fact]
    public void Each_reference_site_matches_the_authored_faces_and_bounds()
    {
        using JsonDocument document = Bodies();
        foreach (JsonElement site in document.RootElement.GetProperty("sites").EnumerateArray())
        {
            string name = site.GetProperty("name").GetString()!;
            FamilyBodyMesh mesh = BuildSite(site);

            Dictionary<string, int> expected = site.GetProperty("faces").EnumerateObject()
                .ToDictionary(p => p.Name, p => p.Value.GetInt32());
            Dictionary<string, int> actual = mesh.Parts.ToDictionary(p => p.Part, p => p.Mesh.Positions.Length / 4);
            Assert.True(expected.OrderBy(p => p.Key).SequenceEqual(actual.OrderBy(p => p.Key)),
                $"{name}: authored {Describe(expected)}, built {Describe(actual)}");

            JsonElement bounds = site.GetProperty("bounds");
            Vector3[] all = [.. mesh.Parts.SelectMany(p => p.Mesh.Positions.ToArray())];
            Assert.Equal(bounds[0][0].GetSingle(), all.Min(p => p.X), 2);
            Assert.Equal(bounds[1][0].GetSingle(), all.Max(p => p.X), 2);
            Assert.Equal(bounds[0][2].GetSingle(), all.Min(p => p.Y), 2);
            Assert.Equal(bounds[1][2].GetSingle(), all.Max(p => p.Y), 2);
            Assert.Equal(-bounds[1][1].GetSingle(), all.Min(p => p.Z), 2);
            Assert.Equal(-bounds[0][1].GetSingle(), all.Max(p => p.Z), 2);
        }
    }

    [Fact]
    public void The_builder_looks_match_the_authored_reference()
    {
        using JsonDocument document = Bodies();
        JsonElement looks = document.RootElement.GetProperty("looks");
        JsonElement mansion = looks.GetProperty("mansion");
        JsonElement slab = looks.GetProperty("slab");

        Assert.Equal(FamilyBodyBuilder.RingWingMetres, document.RootElement.GetProperty("wing_metres").GetSingle());
        Assert.Equal(mansion.GetProperty("bay").GetSingle(), FamilyBodyBuilder.Mansion.Bay);
        Assert.Equal(mansion.GetProperty("attic_window")[0].GetSingle(), FamilyBodyBuilder.Mansion.AtticWindow.W);
        Assert.Equal(mansion.GetProperty("attic_setback").GetSingle(), FamilyBodyBuilder.Mansion.AtticSetback);
        Assert.Equal(mansion.GetProperty("loggia_every").GetInt32(), FamilyBodyBuilder.Mansion.LoggiaEvery);
        Assert.Equal(slab.GetProperty("bay").GetSingle(), FamilyBodyBuilder.PanelSlab.Bay);
        Assert.Equal(slab.GetProperty("gallery_depth").GetSingle(), FamilyBodyBuilder.PanelSlab.GalleryDepth);
        Assert.Equal(slab.GetProperty("piloti_every").GetSingle(), FamilyBodyBuilder.PanelSlab.PilotiEvery);
    }

    [Theory]
    [InlineData("mansion", 56f, 56f, 7)]
    [InlineData("panel-slab", 116f, 54f, 16)]
    public void A_ring_leaves_its_courtyard_open(string variant, float frontage, float depth, int storeys)
    {
        FamilyBodyMesh mesh = FamilyBodyBuilder.BuildMidrise(Midrise(variant), frontage, depth, storeys, ring: true);
        float reach = FamilyBodyBuilder.RingWingMetres + 2f;
        float holeX = (frontage / 2f) - reach, holeZ = (depth / 2f) - reach;

        Assert.All(mesh.Parts.SelectMany(p => p.Mesh.Positions.ToArray()), position =>
            Assert.False(MathF.Abs(position.X) < holeX && MathF.Abs(position.Z) < holeZ,
                $"{variant} draws {position} inside its courtyard"));
    }

    [Fact]
    public void An_attached_flank_has_no_openings()
    {
        FamilyBodyMesh mesh = FamilyBodyBuilder.BuildMidrise(Midrise("mansion"), 40f, 16f, 6, ring: false,
            AttachedSides.Right);
        ShellMesh glass = mesh.Parts.Single(p => p.Part == "glass").Mesh;

        Assert.DoesNotContain(glass.Positions.ToArray(), p => p.X > 19.7f);
        Assert.Contains(glass.Positions.ToArray(), p => p.X < -19.7f);
    }

    [Fact]
    public void Every_triangle_is_finite_and_non_degenerate()
    {
        foreach (string variant in new[] { "mansion", "panel-slab" })
        {
            foreach (bool ring in new[] { false, true })
            {
                FamilyBodyMesh mesh = FamilyBodyBuilder.BuildMidrise(Midrise(variant), 61f, 49f, 4, ring);
                foreach ((string part, ShellMesh shell) in mesh.Parts)
                {
                    ReadOnlySpan<Vector3> positions = shell.Positions;
                    ReadOnlySpan<int> indices = shell.Indices;
                    for (int at = 0; at < indices.Length; at += 3)
                    {
                        Vector3 a = positions[indices[at]], b = positions[indices[at + 1]], c = positions[indices[at + 2]];
                        Assert.True(float.IsFinite(a.X + a.Y + a.Z), $"{variant} {part} has a non-finite vertex");
                        Assert.True(Vector3.Cross(b - a, c - a).LengthSquared() > 1e-8f,
                            $"{variant} ring {ring} {part} has a degenerate triangle at {a}");
                    }
                }
            }
        }
    }

    [Fact]
    public void The_reader_accepts_both_variants()
    {
        Assert.Equal(MidriseVariant.Mansion, Midrise("mansion").Midrise!.Variant);
        Assert.Equal(MidriseVariant.PanelSlab, Midrise("panel-slab").Midrise!.Variant);
    }

    [Theory]
    [InlineData("shape = \"midrise\"", "'variant' is required")]
    [InlineData("shape = \"midrise\"\nvariant = \"point\"", "'variant' must be \"mansion\" or \"panel-slab\"")]
    [InlineData("shape = \"midrise\"\nvariant = \"mansion\"\nbay_metres = 4", "'bay_metres' is only valid for a block body.")]
    [InlineData("shape = \"tower\"\nvariant = \"mansion\"", "'variant' must be \"point\", \"stepped-point\", \"l\" or \"h\"")]
    public void The_reader_refuses_a_malformed_midrise(string body, string message)
    {
        StylePresetResult read = ReadBody(body);

        Assert.Null(read.Preset);
        Assert.Contains(read.Errors, e => e.Message.Contains(message, StringComparison.Ordinal));
    }

    [Fact]
    public void Each_build_entry_point_refuses_a_body_of_another_shape()
    {
        FamilyBody block = ReadBody("bay_metres = 4").Preset!.Families[0].Body!;

        Assert.Throws<ArgumentException>(() => FamilyBodyBuilder.BuildMidrise(block, 40f, 16f, 6, ring: false));
        Assert.Throws<ArgumentException>(() => FamilyBodyBuilder.Build(Midrise("mansion"), 40f, 16f, 6));
    }

    private static FamilyBodyMesh BuildSite(JsonElement site)
    {
        AttachedSides attached = AttachedSides.None;
        foreach (JsonElement side in site.GetProperty("attached").EnumerateArray())
        {
            attached |= side.GetString() == "w" ? AttachedSides.Left : AttachedSides.Right;
        }

        string variant = site.GetProperty("family").GetString() == "mansion" ? "mansion" : "panel-slab";
        return FamilyBodyBuilder.BuildMidrise(Midrise(variant), site.GetProperty("frontage").GetSingle(),
            site.GetProperty("depth").GetSingle(), site.GetProperty("storeys").GetInt32(),
            site.GetProperty("ring").GetBoolean(), attached);
    }

    private static string Describe(Dictionary<string, int> faces) =>
        string.Join(", ", faces.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => $"{p.Key} {p.Value}"));

    private static FamilyBody Midrise(string variant)
    {
        StylePresetResult read = ReadBody($"shape = \"midrise\"\nvariant = \"{variant}\"");
        Assert.Empty(read.Errors);
        return read.Preset!.Families[0].Body!;
    }

    private static StylePresetResult ReadBody(string body) => StylePresetReader.Read([("midrise.toml", $$"""
        [preset]
        name = "midrise"
        era_days = 1
        [[family]]
        id = "midrise"
        kinds = ["dwelling"]
        [family.body]
        library = "library"
        {{body}}
        """)]);

    private static JsonDocument Bodies()
    {
        DirectoryInfo? at = new(AppContext.BaseDirectory);
        while (at is not null && !File.Exists(Path.Combine(at.FullName, "CONTEXT.md"))) at = at.Parent;
        return JsonDocument.Parse(File.ReadAllText(
            Path.Combine(at!.FullName, "art", "midrise-families", "bodies.json")));
    }
}
