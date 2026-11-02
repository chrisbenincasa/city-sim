using System.Numerics;
using System.Text.Json;
using Borough.Appearance;

namespace Borough.Tests.Appearance;

/// <summary>Holds the perimeter forms in <c>FamilyBodyBuilder.BuildMidrise</c> to <c>art/perimeter-forms/bodies.json</c>.</summary>
public sealed class PerimeterBodyTests
{
    [Fact]
    public void Each_reference_site_matches_the_authored_corners_and_bounds()
    {
        using JsonDocument document = Bodies();
        foreach (JsonElement site in document.RootElement.GetProperty("sites").EnumerateArray())
        {
            string name = site.GetProperty("name").GetString()!;
            AssertCorners(name, BuildSite(site), site.GetProperty("corners"));
            AssertBounds(name, BuildSite(site), site.GetProperty("bounds"));
        }
    }

    [Fact]
    public void Each_far_reference_site_matches_the_authored_corners_cells_and_bounds()
    {
        using JsonDocument document = Bodies();
        foreach (JsonElement site in document.RootElement.GetProperty("sites").EnumerateArray())
        {
            string name = site.GetProperty("name").GetString()!;
            FamilyBodyMesh far = BuildSite(site, FamilyBodyDetail.Far);
            AssertCorners(name, far, site.GetProperty("far_corners"));
            AssertBounds(name, far, site.GetProperty("far_bounds"));

            float[] near = MeshBounds(BuildSite(site));
            float[] distant = MeshBounds(far);
            Assert.True(MathF.Abs(near[5] - distant[5]) <= .001f, $"{name}: near top {near[5]} differs from far {distant[5]}");

            ShellMesh facade = far.Parts.Single(p => p.Part == "far-facade").Mesh;
            Dictionary<string, int> expected = site.GetProperty("far_cell_counts").EnumerateObject()
                .ToDictionary(p => p.Name, p => p.Value.GetInt32());
            Dictionary<string, int> actual = Enumerable.Range(0, facade.VertexCount / 4)
                .Where(face => IsGridQuad(facade, face * 4))
                .GroupBy(face => ((int)facade.Uv2s[face * 4].X).ToString(System.Globalization.CultureInfo.InvariantCulture))
                .ToDictionary(g => g.Key, g => g.Count());
            Assert.True(expected.OrderBy(p => p.Key).SequenceEqual(actual.OrderBy(p => p.Key)),
                $"{name}: authored cells {Describe(expected)}, built {Describe(actual)}");
        }
    }

    [Fact]
    public void The_builder_looks_match_the_authored_reference()
    {
        using JsonDocument document = Bodies();
        JsonElement looks = document.RootElement.GetProperty("looks");
        foreach ((string form, string variant) in Forms)
        {
            JsonElement look = looks.GetProperty(form);
            FamilyBodyBuilder.PerimeterLook built = FamilyBodyBuilder.PerimeterLookOf(Midrise(variant).Midrise!);
            Assert.Equal(look.GetProperty("bay").GetSingle(), built.Bay);
            Assert.Equal(look.GetProperty("attic").GetBoolean(), built.Attic);
            if (look.TryGetProperty("window", out JsonElement window))
            {
                Assert.Equal(window[0].GetSingle(), built.Window.W);
                Assert.Equal(window[1].GetSingle(), built.Window.H);
                Assert.Equal(window[2].GetSingle(), built.Window.Sill);
            }

            if (look.TryGetProperty("reveal", out JsonElement reveal)) Assert.Equal(reveal.GetSingle(), built.Reveal);
            if (look.TryGetProperty("attic_setback", out JsonElement setback)) Assert.Equal(setback.GetSingle(), built.AtticSetback);
            if (look.TryGetProperty("parapet", out JsonElement parapet)) Assert.Equal(parapet.GetSingle(), built.Parapet);
            if (look.TryGetProperty("fascia", out JsonElement fascia)) Assert.Equal((fascia[0].GetSingle(), fascia[1].GetSingle()), built.Fascia);
            if (look.TryGetProperty("pitch", out JsonElement pitch)) Assert.Equal(pitch.GetSingle(), built.Pitch);
        }
    }

    [Fact]
    public void A_perimeter_form_refuses_the_mansion_settings()
    {
        StylePresetResult read = ReadBody("shape = \"midrise\"\nvariant = \"ribbon\"\nshops = true");
        Assert.Contains(read.Errors, e => e.Message.Contains("'shops' is only valid for a mansion or panel-slab body.", StringComparison.Ordinal));
    }

    [Fact]
    public void Every_triangle_is_finite_and_non_degenerate()
    {
        foreach ((_, string variant) in Forms)
        {
            foreach (FamilyBodyDetail detail in (FamilyBodyDetail[])[FamilyBodyDetail.Near, FamilyBodyDetail.Far])
            {
                FamilyBodyMesh mesh = FamilyBodyBuilder.BuildMidrise(Midrise(variant), 44f, 24f, 4, ring: false, detail: detail);
                foreach ((string part, ShellMesh shell) in mesh.Parts)
                {
                    for (int i = 0; i < shell.IndexCount; i += 3)
                    {
                        Vector3 a = shell.Positions[shell.Indices[i]], b = shell.Positions[shell.Indices[i + 1]], c = shell.Positions[shell.Indices[i + 2]];
                        Assert.True(float.IsFinite(a.X + a.Y + a.Z), $"{variant} {part}: non-finite corner");
                        Assert.True(Vector3.Cross(b - a, c - a).LengthSquared() > 1e-12f, $"{variant} {part}: degenerate triangle");
                    }
                }
            }
        }
    }

    private static readonly (string Form, string Variant)[] Forms =
    [
        ("brick-loggia", "brick-loggia"), ("timber-frame", "timber-frame"), ("white-render", "white-render"),
        ("gable-front", "gable-front"), ("ribbon", "ribbon"), ("crosswall", "crosswall"),
    ];

    private static FamilyBodyMesh BuildSite(JsonElement site, FamilyBodyDetail detail = FamilyBodyDetail.Near)
    {
        AttachedSides attached = AttachedSides.None;
        foreach (JsonElement side in site.GetProperty("attached").EnumerateArray())
        {
            attached |= side.GetString() == "w" ? AttachedSides.Left : AttachedSides.Right;
        }

        return FamilyBodyBuilder.BuildMidrise(Midrise(site.GetProperty("form").GetString()!), site.GetProperty("frontage").GetSingle(),
            site.GetProperty("depth").GetSingle(), site.GetProperty("storeys").GetInt32(), site.GetProperty("ring").GetBoolean(),
            attached, detail);
    }

    private static void AssertCorners(string name, FamilyBodyMesh mesh, JsonElement corners)
    {
        Dictionary<string, int> expected = corners.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.GetInt32());
        Dictionary<string, int> actual = mesh.Parts.ToDictionary(p => p.Part, p => p.Mesh.VertexCount);
        Assert.True(expected.OrderBy(p => p.Key).SequenceEqual(actual.OrderBy(p => p.Key)),
            $"{name}: authored {Describe(expected)}, built {Describe(actual)}");
    }

    private static void AssertBounds(string name, FamilyBodyMesh mesh, JsonElement bounds)
    {
        float[] actual = MeshBounds(mesh);
        float[] expected = [.. bounds[0].EnumerateArray().Concat(bounds[1].EnumerateArray()).Select(v => v.GetSingle())];
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.True(MathF.Abs(expected[i] - actual[i]) <= .0011f, $"{name}: bound {i} authored {expected[i]}, built {actual[i]}");
        }
    }

    private static float[] MeshBounds(FamilyBodyMesh mesh)
    {
        Vector3[] all = [.. mesh.Parts.SelectMany(p => p.Mesh.Positions.ToArray())];
        return [all.Min(p => p.X), -all.Max(p => p.Z), all.Min(p => p.Y),
            all.Max(p => p.X), -all.Min(p => p.Z), all.Max(p => p.Y)];
    }

    private static bool IsGridQuad(ShellMesh mesh, int at)
    {
        for (int i = 0; i < 4; i++)
        {
            if (MathF.Abs(mesh.Positions[at + i].Y - (mesh.Uvs[at + i].Y * ShellBuilder.StoreyMetres)) > .001f) return false;
        }

        return true;
    }

    private static string Describe(Dictionary<string, int> counts) =>
        string.Join(", ", counts.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => $"{p.Key} {p.Value}"));

    private static FamilyBody Midrise(string variant)
    {
        StylePresetResult read = ReadBody($"shape = \"midrise\"\nvariant = \"{variant}\"");
        Assert.Empty(read.Errors);
        return read.Preset!.Families[0].Body!;
    }

    private static StylePresetResult ReadBody(string body) => StylePresetReader.Read([("perimeter.toml", $$"""
        [preset]
        name = "perimeter"
        era_days = 1
        [[family]]
        id = "perimeter"
        kinds = ["dwelling"]
        [family.body]
        library = "library"
        {{body}}
        """)]);

    private static JsonDocument Bodies()
    {
        DirectoryInfo? at = new(AppContext.BaseDirectory);
        while (at is not null && !File.Exists(Path.Combine(at.FullName, "CONTEXT.md"))) at = at.Parent;
        return JsonDocument.Parse(File.ReadAllText(Path.Combine(at!.FullName, "art", "perimeter-forms", "bodies.json")));
    }
}
