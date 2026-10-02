using System.Numerics;
using System.Text.Json;
using Borough.Appearance;

namespace Borough.Tests.Appearance;

public sealed class TowerBodyTests
{
    [Fact]
    public void The_reference_sites_keep_the_authored_wing_storeys_height_and_floor()
    {
        using JsonDocument document = Bodies();
        foreach (JsonElement site in document.RootElement.GetProperty("sites").EnumerateArray())
        {
            TowerVariant variant = Variant(site.GetProperty("variant").GetString()!);
            int podium = site.GetProperty("podium_storeys").GetInt32();
            FamilyBodyBuilder.TowerLayoutSummary layout =
                FamilyBodyBuilder.SolveTowerLayout(variant, 116f, 116f, 62, podium, 58f, 58f);

            Assert.Equal(site.GetProperty("top_metres").GetSingle(), layout.TopMetres, 3);
            (string Name, int Storeys)[] expected =
            [
                .. site.GetProperty("wings").EnumerateArray()
                    .Select(w => (w[0].GetString()!, w[1].GetInt32())),
            ];
            Assert.Equal(expected, layout.Wings.Select(w => (w.Name, w.Storeys)).ToArray());
            Assert.Equal(site.GetProperty("shaft_floor").GetSingle(), layout.ShaftFloor, 2);
            Assert.InRange(MathF.Abs(layout.DrawnFloor - layout.ShaftFloor) / layout.ShaftFloor, 0f, .01f);
        }
    }

    [Theory]
    [InlineData(TowerVariant.Point)]
    [InlineData(TowerVariant.SteppedPoint)]
    [InlineData(TowerVariant.L)]
    [InlineData(TowerVariant.H)]
    public void A_non_square_site_stays_within_its_footprint_allowance_and_faces_outward(TowerVariant variant)
    {
        FamilyBody body = Tower(variant);
        FamilyBodyBuilder.TowerLayoutSummary layout =
            FamilyBodyBuilder.SolveTowerLayout(variant, 112f, 120f, 64, 2, 56f, 60f);
        FamilyBodyMesh mesh = FamilyBodyBuilder.BuildTower(body, 112f, 120f, 64, 2, 56f, 60f);
        Vector3[] all = [.. mesh.Parts.SelectMany(p => p.Mesh.Positions.ToArray())];

        Assert.InRange(MathF.Abs(layout.DrawnFloor - layout.ShaftFloor) / layout.ShaftFloor, 0f, .01f);
        Assert.InRange(all.Min(p => p.X), -58.5f, -53.5f);
        Assert.InRange(all.Max(p => p.X), 53.5f, 58.5f);
        Assert.InRange(all.Min(p => p.Z), -62.5f, -57.5f);
        Assert.InRange(all.Max(p => p.Z), 57.5f, 62.5f);
        Assert.Equal(0f, all.Min(p => p.Y), 3);
        AssertTowerPolygonsFaceOutward(mesh, layout, 112f, 120f, 2);
    }

    [Fact]
    public void The_outward_check_rejects_a_flipped_face()
    {
        FamilyBodyMesh mesh = FamilyBodyBuilder.BuildTower(Tower(TowerVariant.Point), 116f, 116f, 12, 2, 58f, 58f);
        ShellMesh solid = mesh.Parts.First(p => p.Part == "plinth").Mesh;
        Vector3[] positions = solid.Positions[..24].ToArray();
        Vector3[] normals = solid.Normals[..24].ToArray();
        Array.Reverse(positions, 0, 4);
        for (int i = 0; i < 4; i++) normals[i] = -normals[i];

        Assert.False(SolidFacesPointOutward(positions, normals));
    }

    [Theory]
    [InlineData(TowerVariant.Point)]
    [InlineData(TowerVariant.SteppedPoint)]
    [InlineData(TowerVariant.L)]
    [InlineData(TowerVariant.H)]
    public void Short_shafts_never_draw_non_positive_wings(TowerVariant variant)
    {
        for (int shaftStoreys = 1; shaftStoreys <= 3; shaftStoreys++)
        {
            const int podiumStoreys = 2;
            FamilyBodyBuilder.TowerLayoutSummary layout = FamilyBodyBuilder.SolveTowerLayout(
                variant, 116f, 116f, shaftStoreys + podiumStoreys, podiumStoreys, 58f, 58f);
            FamilyBodyMesh mesh = FamilyBodyBuilder.BuildTower(
                Tower(variant), 116f, 116f, shaftStoreys + podiumStoreys, podiumStoreys, 58f, 58f);

            Assert.All(layout.Wings, wing => Assert.True(wing.Storeys > 0, $"{wing.Name} has {wing.Storeys} storeys"));
            Assert.All(mesh.Parts.SelectMany(p => p.Mesh.Positions.ToArray()), position =>
                Assert.True(IsFinite(position), $"{variant} shaft {shaftStoreys} emitted {position}"));
        }
    }

    [Fact]
    public void Wing_rounding_stays_within_one_requested_shaft_storey()
    {
        foreach (TowerVariant variant in Enum.GetValues<TowerVariant>())
        {
            for (int frontage = 112; frontage <= 120; frontage++)
            {
                for (int depth = 112; depth <= 120; depth++)
                {
                    float shaftFrontage = frontage / 2f;
                    float shaftDepth = depth / 2f;
                    float oneShaftStorey = shaftFrontage * shaftDepth;
                    for (int shaftStoreys = 1; shaftStoreys <= 70; shaftStoreys++)
                    {
                        for (int podiumStoreys = 0; podiumStoreys <= 8; podiumStoreys++)
                        {
                            FamilyBodyBuilder.TowerLayoutSummary layout = FamilyBodyBuilder.SolveTowerLayout(
                                variant, frontage, depth, shaftStoreys + podiumStoreys, podiumStoreys,
                                shaftFrontage, shaftDepth);
                            float difference = MathF.Abs(layout.DrawnFloor - layout.ShaftFloor);
                            float tolerance = Math.Max(layout.ShaftFloor * .01f, oneShaftStorey);
                            Assert.True(difference <= tolerance,
                                $"{variant} {frontage}x{depth}, shaft {shaftStoreys}, podium {podiumStoreys}: " +
                                $"difference {difference} exceeds {tolerance}");
                        }
                    }
                }
            }
        }
    }

    [Fact]
    public void A_small_podium_has_only_finite_non_degenerate_triangles()
    {
        FamilyBodyMesh mesh = FamilyBodyBuilder.BuildTower(Tower(TowerVariant.Point), 5f, 5f, 1, 1, 2.5f, 2.5f);

        foreach ((_, ShellMesh part) in mesh.Parts)
        {
            Assert.All(part.Positions.ToArray(), position => Assert.True(IsFinite(position), $"non-finite position {position}"));
            Assert.All(part.Normals.ToArray(), normal => Assert.True(IsFinite(normal), $"non-finite normal {normal}"));
            ReadOnlySpan<Vector3> positions = part.Positions;
            ReadOnlySpan<int> indices = part.Indices;
            for (int triangle = 0; triangle < indices.Length; triangle += 3)
            {
                Vector3 cross = Vector3.Cross(
                    positions[indices[triangle + 1]] - positions[indices[triangle]],
                    positions[indices[triangle + 2]] - positions[indices[triangle]]);
                Assert.True(cross.LengthSquared() > 1e-8f, $"degenerate triangle at index {triangle / 3}");
            }
        }
    }

    [Fact]
    public void The_builder_facade_parameters_match_the_authored_reference()
    {
        using JsonDocument document = Bodies();
        JsonElement authored = document.RootElement.GetProperty("facades");
        Assert.Equal(authored.EnumerateObject().Select(p => p.Name).Order(),
            FamilyBodyBuilder.TowerFacades.Keys.Order());
        foreach (JsonProperty entry in authored.EnumerateObject())
        {
            JsonElement expected = entry.Value;
            FamilyBodyBuilder.TowerFacadeParameters actual = FamilyBodyBuilder.TowerFacades[entry.Name];
            Assert.Equal(expected.GetProperty("spandrel").GetSingle(), actual.Spandrel, 6);
            Assert.Equal(expected.GetProperty("spandrel_part").GetString(), actual.SpandrelPart);
            Assert.Equal(expected.GetProperty("out").GetSingle(), actual.Out, 6);
            Assert.Equal(expected.GetProperty("fin_every").GetSingle(), actual.FinEvery, 6);
            Assert.Equal(expected.GetProperty("fin")[0].GetSingle(), actual.FinWidth, 6);
            Assert.Equal(expected.GetProperty("fin")[1].GetSingle(), actual.FinFront, 6);
            Assert.Equal(expected.GetProperty("fin")[2].GetSingle(), actual.FinBack, 6);
            Assert.Equal(expected.GetProperty("fin_part").GetString(), actual.FinPart);
        }
    }

    [Fact]
    public void The_reader_accepts_a_tower_and_its_new_parts()
    {
        StylePresetResult read = StylePresetReader.Read([("tower.toml", """
            [preset]
            name = "tower"
            era_days = 1

            [[family]]
            id = "point"
            kinds = ["dwelling"]

            [family.body]
            library = "tall-families/library"
            shape = "tower"
            variant = "point"
            tile_metres = { paving = [2, 2] }
            materials = { spandrel = "metal-panels" }

            [[family.paint]]
            reveal = "262b2f"
            tree = "6f7f63"
            """)]);

        Assert.Empty(read.Errors);
        FamilyBody body = Assert.Single(read.Preset!.Families).Body!;
        Assert.Equal(TowerVariant.Point, body.Tower!.Variant);
        Assert.Equal(0f, body.BayMetres);
        Assert.Equal((2f, 2f), body.TileMetres["paving"]);
        Assert.Equal("metal-panels", body.Materials["spandrel"]);
    }

    [Theory]
    [InlineData("", "'variant' is required")]
    [InlineData("variant = \"spire\"", "'variant' must be \"point\", \"stepped-point\", \"l\" or \"h\"")]
    public void A_tower_needs_a_known_variant(string variant, string message)
    {
        StylePresetResult read = ReadBody($"shape = \"tower\"\n{variant}");

        Assert.Null(read.Preset);
        Assert.Contains(read.Errors, e => e.Message.Contains(message, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("gable_degrees = 30", "'gable_degrees' is only valid for a block body.")]
    [InlineData("bay_metres = 4", "'bay_metres' is only valid for a block body.")]
    public void A_tower_refuses_a_block_key_at_that_keys_line(string key, string message)
    {
        StylePresetResult read = ReadBody($"shape = \"tower\"\nvariant = \"h\"\n{key}");

        AppearanceDiagnostic refusal = Assert.Single(read.Errors);
        Assert.Equal(11, refusal.Line);
        Assert.Equal(message, refusal.Message);
    }

    [Fact]
    public void A_block_refuses_a_tower_variant()
    {
        StylePresetResult read = ReadBody("bay_metres = 4\nvariant = \"point\"");

        Assert.Null(read.Preset);
        Assert.Contains(read.Errors, e => e.Message.Contains("'variant' is only valid when 'shape' is \"tower\"", StringComparison.Ordinal));
    }

    [Fact]
    public void Each_build_entry_point_refuses_the_other_body_shape()
    {
        FamilyBody block = ReadBody("bay_metres = 4").Preset!.Families[0].Body!;
        FamilyBody tower = Tower(TowerVariant.Point);

        Assert.Throws<ArgumentException>(() => FamilyBodyBuilder.BuildTower(block, 116f, 116f, 10, 2, 58f, 58f));
        Assert.Throws<ArgumentException>(() => FamilyBodyBuilder.Build(tower, 116f, 116f, 10));
    }

    private static void AssertTowerPolygonsFaceOutward(FamilyBodyMesh mesh,
        FamilyBodyBuilder.TowerLayoutSummary layout, float frontage, float depth, int podiumStoreys)
    {
        foreach ((string name, ShellMesh part) in mesh.Parts)
        {
            if (name == "glass")
            {
                AssertGlassFacesPointOutward(part, layout, frontage, depth, podiumStoreys);
            }
            else if (name == "tree")
            {
                Assert.True(TreeFacesPointOutward(part.Positions, part.Normals), "a tree face points inward");
            }
            else
            {
                Assert.True(SolidFacesPointOutward(part.Positions, part.Normals), $"a {name} solid face points inward");
            }
        }
    }

    private static void AssertGlassFacesPointOutward(ShellMesh glass,
        FamilyBodyBuilder.TowerLayoutSummary layout, float frontage, float depth, int podiumStoreys)
    {
        ReadOnlySpan<Vector3> positions = glass.Positions;
        ReadOnlySpan<Vector3> normals = glass.Normals;
        Assert.Equal(0, positions.Length % 4);
        for (int offset = 0; offset < positions.Length; offset += 4)
        {
            Vector3 centre = (positions[offset] + positions[offset + 1] + positions[offset + 2] + positions[offset + 3]) / 4f;
            float low = Math.Min(Math.Min(positions[offset].Y, positions[offset + 1].Y),
                Math.Min(positions[offset + 2].Y, positions[offset + 3].Y));
            float high = Math.Max(Math.Max(positions[offset].Y, positions[offset + 1].Y),
                Math.Max(positions[offset + 2].Y, positions[offset + 3].Y));
            Vector3? expected = null;
            foreach (FamilyBodyBuilder.TowerWingSummary wing in layout.Wings)
            {
                if (low < wing.BaseMetres - 1e-3f || high > wing.TopMetres + 1e-3f) continue;
                expected = RectFaceNormal(centre, wing.X0, wing.Y0, wing.X1, wing.Y1, .3f);
                if (expected is not null) break;
            }

            if (expected is null && high <= (podiumStoreys * 3.5f) + 1e-3f)
            {
                expected = RectFaceNormal(centre, -frontage / 2f, -depth / 2f, frontage / 2f, depth / 2f, .3f)
                    ?? RectFaceNormal(centre, -frontage / 2f, -depth / 2f, frontage / 2f, depth / 2f, .4f);
            }

            Assert.True(expected is not null, $"glass face at {centre} belongs to no wing or podium");
            Assert.True(WindingMatchesNormal(positions, normals[offset], offset),
                $"glass face at {centre} has inward winding");
            Assert.True(Vector3.Dot(normals[offset], expected.Value) > .99f,
                $"glass face at {centre} points {normals[offset]} instead of {expected.Value}");
        }
    }

    private static Vector3? RectFaceNormal(Vector3 centre,
        float x0, float y0, float x1, float y1, float inset)
    {
        const float Epsilon = 1e-3f;
        if (-y1 - Epsilon <= centre.Z && centre.Z <= -y0 + Epsilon)
        {
            if (MathF.Abs(centre.X - (x0 + inset)) <= Epsilon) return -Vector3.UnitX;
            if (MathF.Abs(centre.X - (x1 - inset)) <= Epsilon) return Vector3.UnitX;
        }

        if (x0 - Epsilon <= centre.X && centre.X <= x1 + Epsilon)
        {
            if (MathF.Abs(centre.Z - (-y0 - inset)) <= Epsilon) return Vector3.UnitZ;
            if (MathF.Abs(centre.Z - (-y1 + inset)) <= Epsilon) return -Vector3.UnitZ;
        }

        return null;
    }

    private static bool SolidFacesPointOutward(ReadOnlySpan<Vector3> positions, ReadOnlySpan<Vector3> normals)
    {
        if (positions.Length != normals.Length || positions.Length % 24 != 0) return false;
        for (int solid = 0; solid < positions.Length; solid += 24)
        {
            Vector3 low = positions[solid];
            Vector3 high = positions[solid];
            for (int i = 1; i < 24; i++)
            {
                low = Vector3.Min(low, positions[solid + i]);
                high = Vector3.Max(high, positions[solid + i]);
            }

            Vector3 solidCentre = (low + high) / 2f;
            for (int face = 0; face < 24; face += 4)
            {
                int at = solid + face;
                Vector3 faceCentre = (positions[at] + positions[at + 1] + positions[at + 2] + positions[at + 3]) / 4f;
                Vector3 outward = faceCentre - solidCentre;
                if (!WindingMatchesNormal(positions, normals[at], at)
                    || outward.LengthSquared() <= 1e-8f
                    || Vector3.Dot(normals[at], outward) <= 0f)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static bool TreeFacesPointOutward(ReadOnlySpan<Vector3> positions, ReadOnlySpan<Vector3> normals)
    {
        if (positions.Length != normals.Length || positions.Length % 24 != 0) return false;
        for (int tree = 0; tree < positions.Length; tree += 24)
        {
            Vector3 low = positions[tree];
            Vector3 high = positions[tree];
            for (int i = 1; i < 24; i++)
            {
                low = Vector3.Min(low, positions[tree + i]);
                high = Vector3.Max(high, positions[tree + i]);
            }

            Vector3 treeCentre = (low + high) / 2f;
            for (int face = 0; face < 24; face += 3)
            {
                int at = tree + face;
                Vector3 faceCentre = (positions[at] + positions[at + 1] + positions[at + 2]) / 3f;
                if (!WindingMatchesNormal(positions, normals[at], at)
                    || Vector3.Dot(normals[at], faceCentre - treeCentre) <= 0f)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static bool WindingMatchesNormal(ReadOnlySpan<Vector3> positions, Vector3 normal, int at)
    {
        Vector3 winding = Vector3.Cross(positions[at + 2] - positions[at], positions[at + 1] - positions[at]);
        return Vector3.Dot(winding, normal) > 0f;
    }

    private static bool IsFinite(Vector3 value) =>
        float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z);

    private static StylePresetResult ReadBody(string body) => StylePresetReader.Read([("tower.toml", $$"""
        [preset]
        name = "tower"
        era_days = 1
        [[family]]
        id = "tower"
        kinds = ["dwelling"]
        [family.body]
        library = "library"
        {{body}}
        """)]);

    private static FamilyBody Tower(TowerVariant variant)
    {
        string name = variant switch
        {
            TowerVariant.Point => "point",
            TowerVariant.SteppedPoint => "stepped-point",
            TowerVariant.L => "l",
            TowerVariant.H => "h",
            _ => throw new ArgumentOutOfRangeException(nameof(variant)),
        };
        StylePresetResult read = ReadBody($"shape = \"tower\"\nvariant = \"{name}\"");
        Assert.Empty(read.Errors);
        return read.Preset!.Families[0].Body!;
    }

    private static TowerVariant Variant(string value) => value switch
    {
        "point" => TowerVariant.Point,
        "stepped-point" => TowerVariant.SteppedPoint,
        "l" => TowerVariant.L,
        "h" => TowerVariant.H,
        _ => throw new ArgumentOutOfRangeException(nameof(value)),
    };

    private static JsonDocument Bodies() => JsonDocument.Parse(
        File.ReadAllText(Path.Combine(RepoRoot(), "art", "tall-families", "bodies.json")));

    private static string RepoRoot()
    {
        DirectoryInfo? at = new(AppContext.BaseDirectory);
        while (at is not null && !File.Exists(Path.Combine(at.FullName, "CLAUDE.md"))) at = at.Parent;
        Assert.NotNull(at);
        return at!.FullName;
    }
}
