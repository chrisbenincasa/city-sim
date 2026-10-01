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
    public void Each_far_reference_site_matches_the_authored_faces_and_bounds()
    {
        using JsonDocument document = Bodies();
        foreach (JsonElement site in document.RootElement.GetProperty("sites").EnumerateArray())
        {
            string name = site.GetProperty("name").GetString()!;
            FamilyBodyMesh mesh = BuildSite(site, FamilyBodyDetail.Far);
            Dictionary<string, int> expected = site.GetProperty("far_faces").EnumerateObject()
                .ToDictionary(p => p.Name, p => p.Value.GetInt32());
            Dictionary<string, int> actual = mesh.Parts.ToDictionary(p => p.Part, p => p.Mesh.Positions.Length / 4);
            Assert.True(expected.OrderBy(p => p.Key).SequenceEqual(actual.OrderBy(p => p.Key)),
                $"{name}: authored {Describe(expected)}, built {Describe(actual)}");
            AssertBounds(name, mesh, site.GetProperty("far_bounds"));

            float[] nearBounds = MeshBounds(BuildSite(site));
            float[] farBounds = MeshBounds(mesh);
            foreach (int end in new[] { 2, 5 })
            {
                Assert.True(MathF.Abs(nearBounds[end] - farBounds[end]) <= .001f,
                    $"{name}: near height bound {nearBounds[end]} differs from far {farBounds[end]}");
            }
        }
    }

    [Fact]
    public void Far_facades_keep_the_near_bay_and_storey_grid()
    {
        using JsonDocument document = Bodies();
        foreach (JsonElement site in document.RootElement.GetProperty("sites").EnumerateArray())
        {
            string name = site.GetProperty("name").GetString()!;
            FamilyBodyMesh near = BuildSite(site);
            ShellMesh facade = BuildSite(site, FamilyBodyDetail.Far).Parts.Single(p => p.Part == "far-facade").Mesh;
            AssertStoreyGrid(name, facade);
            foreach ((Vector3 centre, Vector3 normal) in OpeningCentres(near))
            {
                Assert.True(TryFacadeUv(facade, (centre, normal), out Vector2 uv), $"{name}: no far wall contains opening at {centre}");
                Assert.True(uv.X > MathF.Floor(uv.X) + 1e-3f && uv.X < MathF.Ceiling(uv.X) - 1e-3f,
                    $"{name}: opening at {centre} lies on far bay line u={uv.X}");
            }
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

    [Fact]
    public void The_reader_takes_a_midrise_bodys_settings()
    {
        MidriseBody mansion = Midrise("mansion", "module_metres = 3.9\nstreet_openings = \"balconies\"\nshops = true\nattic = false").Midrise!;
        MidriseBody slab = Midrise("panel-slab", "street_openings = \"windows\"\ngalleries = false").Midrise!;

        Assert.Equal(new MidriseBody(MidriseVariant.Mansion, 3.9f, StreetOpenings.Balconies, Shops: true, Attic: false), mansion);
        Assert.Equal(new MidriseBody(MidriseVariant.PanelSlab, null, StreetOpenings.Windows, Galleries: false), slab);
    }

    [Theory]
    [InlineData("shape = \"midrise\"\nvariant = \"mansion\"\nstreet_openings = \"bays\"", "'street_openings' must be \"loggias\", \"balconies\" or \"windows\".")]
    [InlineData("shape = \"midrise\"\nvariant = \"mansion\"\ngalleries = false", "'galleries' is only valid for a panel-slab body.")]
    [InlineData("shape = \"midrise\"\nvariant = \"panel-slab\"\nattic = false", "'attic' is only valid for a mansion body.")]
    [InlineData("shape = \"tower\"\nvariant = \"h\"\nshops = true", "'shops' is only valid for a mid-rise body.")]
    [InlineData("bay_metres = 4\nmodule_metres = 3", "'module_metres' is only valid for a mid-rise body.")]
    public void The_reader_refuses_a_misplaced_midrise_setting(string body, string message)
    {
        StylePresetResult read = ReadBody(body);

        Assert.Null(read.Preset);
        Assert.Contains(read.Errors, e => e.Message == message);
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

    private static FamilyBodyMesh BuildSite(JsonElement site, FamilyBodyDetail detail = FamilyBodyDetail.Near)
    {
        AttachedSides attached = AttachedSides.None;
        foreach (JsonElement side in site.GetProperty("attached").EnumerateArray())
        {
            attached |= side.GetString() == "w" ? AttachedSides.Left : AttachedSides.Right;
        }

        string variant = site.GetProperty("family").GetString() == "mansion" ? "mansion" : "panel-slab";
        JsonElement settings = site.GetProperty("settings");
        FamilyBody body = Midrise(variant);
        body = body with
        {
            Midrise = body.Midrise! with
            {
                ModuleMetres = settings.GetProperty("bay").GetSingle(),
                Street = Enum.Parse<StreetOpenings>(settings.GetProperty("street_openings").GetString()!, ignoreCase: true),
                Shops = settings.GetProperty("shops").GetBoolean(),
                Attic = !settings.TryGetProperty("attic", out JsonElement attic) || attic.GetBoolean(),
                Galleries = !settings.TryGetProperty("galleries", out JsonElement galleries) || galleries.GetBoolean(),
            },
        };
        return FamilyBodyBuilder.BuildMidrise(body, site.GetProperty("frontage").GetSingle(),
            site.GetProperty("depth").GetSingle(), site.GetProperty("storeys").GetInt32(),
            site.GetProperty("ring").GetBoolean(), attached, detail);
    }

    private static void AssertBounds(string name, FamilyBodyMesh mesh, JsonElement bounds)
    {
        float[] actual = MeshBounds(mesh);
        float[] expected = [.. bounds[0].EnumerateArray().Concat(bounds[1].EnumerateArray()).Select(v => v.GetSingle())];
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.True(MathF.Abs(expected[i] - actual[i]) <= .0011f,
                $"{name}: bound {i} authored {expected[i]}, built {actual[i]}");
        }
    }

    private static float[] MeshBounds(FamilyBodyMesh mesh)
    {
        Vector3[] all = [.. mesh.Parts.SelectMany(p => p.Mesh.Positions.ToArray())];
        return [all.Min(p => p.X), -all.Max(p => p.Z), all.Min(p => p.Y),
            all.Max(p => p.X), -all.Min(p => p.Z), all.Max(p => p.Y)];
    }

    private static IEnumerable<(Vector3 Centre, Vector3 Normal)> OpeningCentres(FamilyBodyMesh mesh)
    {
        foreach ((string part, ShellMesh shell) in mesh.Parts)
        {
            if (part is not ("glass" or "door")) continue;
            for (int at = 0; at < shell.VertexCount; at += 4)
            {
                yield return ((shell.Positions[at] + shell.Positions[at + 1]
                    + shell.Positions[at + 2] + shell.Positions[at + 3]) / 4f, shell.Normals[at]);
            }
        }
    }

    private static void AssertStoreyGrid(string name, ShellMesh facade)
    {
        bool found = false;
        for (int at = 0; at < facade.VertexCount; at += 4)
        {
            if (!IsGridQuad(facade, at)) continue;
            found = true;
            for (int i = 0; i < 4; i++)
            {
                Assert.True(MathF.Abs(facade.Positions[at + i].Y - (facade.Uvs[at + i].Y * ShellBuilder.StoreyMetres)) <= .001f,
                    $"{name}: v={facade.Uvs[at + i].Y} misses height {facade.Positions[at + i].Y}");
            }
        }

        Assert.True(found, $"{name}: no gridded far facade quads");
    }

    private static bool TryFacadeUv(ShellMesh facade, (Vector3 Centre, Vector3 Normal) opening, out Vector2 uv)
    {
        uv = default;
        float nearest = float.MaxValue;
        bool found = false;
        for (int at = 0; at < facade.VertexCount; at += 4)
        {
            if (!IsGridQuad(facade, at) || Vector3.Dot(facade.Normals[at], opening.Normal) < .99f) continue;
            Vector3 p0 = facade.Positions[at], e1 = facade.Positions[at + 1] - p0,
                e3 = facade.Positions[at + 3] - p0;
            float distance = Vector3.Dot(opening.Centre - p0, facade.Normals[at]);
            if (MathF.Abs(distance) > 3f || MathF.Abs(distance) >= nearest) continue;
            Vector3 projected = opening.Centre - (facade.Normals[at] * distance);
            float t = Vector3.Dot(projected - p0, e1) / e1.LengthSquared();
            float s = Vector3.Dot(projected - p0, e3) / e3.LengthSquared();
            if (t < -1e-3f || t > 1.001f || s < -1e-3f || s > 1.001f) continue;
            uv = facade.Uvs[at] + ((facade.Uvs[at + 1] - facade.Uvs[at]) * t)
                + ((facade.Uvs[at + 3] - facade.Uvs[at]) * s);
            nearest = MathF.Abs(distance);
            found = true;
        }

        return found;
    }

    private static bool IsGridQuad(ShellMesh mesh, int at)
    {
        for (int i = 0; i < 4; i++)
        {
            if (MathF.Abs(mesh.Positions[at + i].Y - (mesh.Uvs[at + i].Y * ShellBuilder.StoreyMetres)) > .001f) return false;
        }

        return true;
    }

    private static string Describe(Dictionary<string, int> faces) =>
        string.Join(", ", faces.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => $"{p.Key} {p.Value}"));

    private static FamilyBody Midrise(string variant, string settings = "")
    {
        StylePresetResult read = ReadBody($"shape = \"midrise\"\nvariant = \"{variant}\"\n{settings}");
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
