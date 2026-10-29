using System.Numerics;
using Borough.Appearance;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Space;

namespace Borough.Tests.Appearance;

public sealed class WideHouseBodyTests
{
    private static readonly WorldKey World = WorldKey.FromSeed(11);

    public static TheoryData<string> Forms =>
        ["staggered-gables", "flat-setback", "sawtooth", "pavilions", "span", "butterfly", "monopitch", "low-hip",
         "deep-flat", "deep-gables", "deep-sawtooth", "deep-span"];

    public static TheoryData<string> PitchedForms =>
        ["staggered-gables", "sawtooth", "pavilions", "span", "butterfly", "monopitch", "low-hip", "deep-gables", "deep-sawtooth", "deep-span"];

    [Theory]
    [MemberData(nameof(Forms))]
    public void The_same_seed_builds_the_same_row(string form)
    {
        FamilyBody body = House(form);
        (float frontage, float depth) = Lot(form);

        Assert.Equal(Describe(FamilyBodyBuilder.BuildHouse(body, frontage, depth, 2, 17)),
            Describe(FamilyBodyBuilder.BuildHouse(body, frontage, depth, 2, 17)));
    }

    [Theory]
    [MemberData(nameof(Forms))]
    public void Different_seeds_build_different_rows_on_one_lot(string form)
    {
        FamilyBody body = House(form);
        (float frontage, float depth) = Lot(form);

        int distinct = Enumerable.Range(0, 16).Select(seed => Describe(FamilyBodyBuilder.BuildHouse(body, frontage, depth, 2, seed))).Distinct().Count();

        Assert.True(distinct >= 12, $"{form}: 16 seeds built {distinct} distinct rows");
    }

    [Theory]
    [MemberData(nameof(PitchedForms))]
    public void A_row_takes_another_draw_s_pitch_and_keeps_its_own_with_its_own_draw(string form)
    {
        FamilyBody body = House(form);
        float depth = form.StartsWith("deep", StringComparison.Ordinal) ? 32f : 12f;
        string own = Describe(FamilyBodyBuilder.BuildHouse(body, 40f, depth, 2, 17));

        Assert.Equal(own, Describe(FamilyBodyBuilder.BuildHouse(body, 40f, depth, 2, 17, 17)));
        Assert.Contains(Enumerable.Range(0, 8), pitch => Describe(FamilyBodyBuilder.BuildHouse(body, 40f, depth, 2, 17, pitch)) != own);
        Assert.Contains(Enumerable.Range(0, 8), pitch => FamilyBodyBuilder.HousePitchDegrees(Variant(body), pitch, 40f, depth, 2)
            != FamilyBodyBuilder.HousePitchDegrees(Variant(body), 17, 40f, depth, 2));
    }

    /// <remarks>Door canopies stand 1 m out from the street wall, and the low hip's gutters 0.89 m past its ends.</remarks>
    [Theory]
    [MemberData(nameof(Forms))]
    public void Every_draw_stays_on_its_lot_and_faces_the_way_it_is_wound(string form)
    {
        FamilyBody body = House(form);
        foreach ((float frontage, float depth, int storeys) in Lots(form))
        {
            for (int seed = 0; seed < FamilyBodyBuilder.HouseSeeds; seed += 7)
            {
                FamilyBodyMesh mesh = FamilyBodyBuilder.BuildHouse(body, frontage, depth, storeys, seed);
                foreach ((string part, ShellMesh shell) in mesh.Parts)
                {
                    string at = $"{form} {frontage}x{depth}x{storeys} seed {seed} {part}";
                    ReadOnlySpan<Vector3> positions = shell.Positions;
                    foreach (Vector3 p in positions)
                    {
                        Assert.True(MathF.Abs(p.X) <= (frontage / 2f) + .9f, at);
                        Assert.True(MathF.Abs(p.Z) <= (depth / 2f) + 1.1f, at);
                        Assert.True(p.Y >= 0f && p.Y < (storeys * ShellBuilder.StoreyMetres) + 12f, at);
                    }

                    ReadOnlySpan<int> indices = shell.Indices;
                    for (int t = 0; t < indices.Length; t += 3)
                    {
                        Vector3 a = positions[indices[t]], b = positions[indices[t + 1]], c = positions[indices[t + 2]];
                        Assert.True(Vector3.Dot(Vector3.Cross(b - a, c - a), shell.Normals[indices[t]]) <= 1e-6f, $"{at}: a triangle is wound against its normal");
                    }
                }
            }
        }
    }

    /// <summary>The shell culls back faces, so a roof wound inward would leave a hole seen from above.</summary>
    [Theory]
    [MemberData(nameof(Forms))]
    public void Seen_from_above_every_row_shows_the_face_of_its_roofs(string form)
    {
        FamilyBody body = House(form);
        foreach ((float frontage, float depth, int storeys) in Lots(form))
        {
            for (int seed = 0; seed < FamilyBodyBuilder.HouseSeeds; seed += 31)
            {
                FamilyBodyMesh mesh = FamilyBodyBuilder.BuildHouse(body, frontage, depth, storeys, seed);
                for (int i = 0; i < 9; i++)
                {
                    for (int j = 0; j < 5; j++)
                    {
                        float x = (-frontage / 2f) + .37f + ((frontage - .74f) * i / 8f);
                        float z = (-depth / 2f) + .37f + ((depth - .74f) * j / 4f);
                        Assert.True(TopFacesUp(mesh, x, z), $"{form} {frontage}x{depth}x{storeys} seed {seed}: the top surface over ({x:F2}, {z:F2}) faces down");
                    }
                }
            }
        }
    }

    [Theory]
    [InlineData("staggered-gables")]
    [InlineData("flat-setback")]
    [InlineData("pavilions")]
    [InlineData("deep-sawtooth")]
    public void Contemporary_rows_roof_in_standing_seam_and_clad_their_panels_in_seam_or_tile(string form)
    {
        FamilyBody body = House(form);
        (float frontage, float depth) = Lot(form);
        string[][] parts = [.. Enumerable.Range(0, 64).Select(seed => FamilyBodyBuilder.BuildHouse(body, frontage, depth, 3, seed).Parts.Select(p => p.Part).ToArray())];

        if (form != "flat-setback") Assert.All(parts, p => Assert.Contains("roof@standing-seam", p));
        Assert.All(parts, p => Assert.DoesNotContain(p, part => part.StartsWith("roof", StringComparison.Ordinal) && part != "roof@standing-seam"));
        Assert.Contains(parts, p => p.Contains("wall@concrete-tile-facade"));
        Assert.Contains(parts, p => p.Contains("wall@standing-seam"));
    }

    [Theory]
    [InlineData("span")]
    [InlineData("butterfly")]
    public void Mid_century_colored_panels_are_painted_boards(string form)
    {
        FamilyBody body = House(form);
        (float frontage, float depth) = Lot(form);

        Assert.Contains(Enumerable.Range(0, 64), seed =>
            FamilyBodyBuilder.BuildHouse(body, frontage, depth, 2, seed).Parts.Any(p => p.Part == "wall@white-planks-clean"));
    }

    [Theory]
    [InlineData("staggered-gables")]
    [InlineData("sawtooth")]
    [InlineData("pavilions")]
    [InlineData("deep-gables")]
    public void A_metal_roof_s_ridge_cap_stands_on_its_ridge(string form)
    {
        FamilyBody body = House(form);
        for (int seed = 0; seed < FamilyBodyBuilder.HouseSeeds; seed += 37)
        {
            FamilyBodyMesh mesh = FamilyBodyBuilder.BuildHouse(body, 40f, form.StartsWith("deep", StringComparison.Ordinal) ? 34f : 16f, 2, seed);
            float cap = Top(mesh, "trim") - Top(mesh, "roof@standing-seam");

            Assert.True(MathF.Abs(cap - .05f) < .005f, $"{form} seed {seed}: the cap stands {cap:F3} m above the ridge");
        }
    }

    [Theory]
    [InlineData("span")]
    [InlineData("butterfly")]
    [InlineData("monopitch")]
    [InlineData("deep-span")]
    public void A_tiled_roof_s_verges_rise_to_its_top_edge(string form)
    {
        FamilyBody body = House(form);
        for (int seed = 0; seed < FamilyBodyBuilder.HouseSeeds; seed += 37)
        {
            FamilyBodyMesh mesh = FamilyBodyBuilder.BuildHouse(body, 40f, form.StartsWith("deep", StringComparison.Ordinal) ? 34f : 16f, 2, seed);
            float roof = mesh.Parts.Where(p => p.Part.StartsWith("roof@roofing-tiles", StringComparison.Ordinal)).Max(p => Top(mesh, p.Part));

            Assert.True(Top(mesh, "trim") > roof - .1f, $"{form} seed {seed}: the trim stops {roof - Top(mesh, "trim"):F2} m below the roof's top");
        }
    }

    [Fact]
    public void A_low_hip_hangs_its_gutters_outside_its_eaves_on_every_side()
    {
        FamilyBodyMesh mesh = FamilyBodyBuilder.BuildHouse(House("low-hip"), 40f, 16f, 2, 5);
        Vector3[] roof = [.. mesh.Parts.Where(p => p.Part.StartsWith("roof@", StringComparison.Ordinal)).SelectMany(p => p.Mesh.Positions.ToArray())];
        Vector3[] trim = [.. mesh.Parts.Single(p => p.Part == "trim").Mesh.Positions.ToArray()];

        Assert.True(trim.Min(p => p.X) < roof.Min(p => p.X) && trim.Max(p => p.X) > roof.Max(p => p.X));
        Assert.True(trim.Min(p => p.Z) < roof.Min(p => p.Z) && trim.Max(p => p.Z) > roof.Max(p => p.Z));
    }

    [Fact]
    public void A_deep_row_runs_its_outriggers_to_the_back_of_its_lot()
    {
        foreach (string form in (string[])["deep-flat", "deep-gables", "deep-sawtooth", "deep-span"])
        {
            FamilyBodyMesh mesh = FamilyBodyBuilder.BuildHouse(House(form), 40f, 34f, 2, 3);
            float back = mesh.Parts.SelectMany(p => p.Mesh.Positions.ToArray()).Min(p => p.Z);

            Assert.True(back < -15f, $"{form}: the outriggers reach {back:F1} m");
        }
    }

    [Theory]
    [InlineData("terrace")]
    [InlineData("front-gables")]
    [InlineData("villa")]
    public void A_traditional_wide_form_is_wide_only_when_it_says_so(string variant)
    {
        Assert.False(House(variant).House!.Wide);
        Assert.True(House(variant, "wide = true").House!.Wide);
        Assert.False(House(variant, "wide = false").House!.Wide);
    }

    [Theory]
    [MemberData(nameof(Forms))]
    public void Every_contemporary_and_mid_century_form_is_wide(string form)
    {
        Assert.True(House(form).House!.Wide);
    }

    [Theory]
    [InlineData("shape = \"house\"\nvariant = \"hipped\"\nwide = true", "'wide' is only valid on a \"terrace\"")]
    [InlineData("shape = \"house\"\nvariant = \"sawtooth\"\nwide = true", "always wide")]
    [InlineData("bay_metres = 4\nwide = true", "'wide' is only valid for a house body")]
    [InlineData("shape = \"house\"\nvariant = \"villa\"\nwide = 1", "'wide' must be true or false")]
    public void A_misplaced_wide_key_is_refused(string body, string message)
    {
        StylePresetResult read = ReadBody(body);

        Assert.Null(read.Preset);
        Assert.Contains(read.Errors, e => e.Message.Contains(message, StringComparison.Ordinal));
    }

    [Fact]
    public void Wide_houses_stand_in_runs_that_share_a_form_and_differ_from_their_neighbors()
    {
        StylePreset preset = TestStreet();
        var forms = new HashSet<HouseVariant>();
        int pairs = 0;
        for (ulong face = 1; face <= 300; face++)
        {
            (AppearanceFamily Family, Terrace Run)?[] lots = [.. Enumerable.Range(0, 14).Select(i =>
            {
                BuildingFacts facts = Facts((face * 100) + (ulong)i, face, (36 * i) + 1);
                AppearanceFamily family = FamilyPicker.Pick(preset, World, facts).Family!;
                return family.Body?.House is { Wide: true }
                    ? ((AppearanceFamily, Terrace)?)(family, FamilyPicker.WideHouseRunOf(preset, World, facts)!.Value)
                    : null;
            })];

            for (int i = 0; i < lots.Length; i++)
            {
                if (lots[i] is not { } here) continue;
                forms.Add(here.Family.Body!.House!.Variant);
                if (i == 0 || lots[i - 1] is not { } before) continue;
                pairs++;
                bool inside = here.Run.Cell == before.Run.Cell;
                Assert.Equal(inside, here.Family.Body!.House!.Variant == before.Family.Body!.House!.Variant);
            }
        }

        Assert.True(pairs > 100, $"only {pairs} neighboring wide pairs");
        Assert.Equal(11, forms.Count);
    }

    [Fact]
    public void Narrow_houses_pick_as_they_did_without_the_wide_forms()
    {
        StylePreset preset = TestStreet();
        StylePreset narrow = preset with
        {
            Families = [.. preset.Families.Select(f => f.Body?.House is { Wide: true } ? f with { Fallback = true } : f)],
        };

        for (ulong face = 1; face <= 60; face++)
        {
            for (int i = 0; i < 10; i++)
            {
                int frontage = 8 + (i % 4);
                BuildingFacts facts = Facts((face * 100) + (ulong)i, face, (frontage * i) + 1, frontage, 8 + (i % 9), 1 + (i % 3));
                Assert.Equal(FamilyPicker.Pick(narrow, World, facts).Family?.Id, FamilyPicker.Pick(preset, World, facts).Family?.Id);
            }
        }
    }

    private static bool TopFacesUp(FamilyBodyMesh mesh, float x, float z)
    {
        float top = float.NegativeInfinity;
        bool up = false;
        foreach ((_, ShellMesh shell) in mesh.Parts)
        {
            ReadOnlySpan<Vector3> positions = shell.Positions;
            ReadOnlySpan<int> indices = shell.Indices;
            for (int t = 0; t < indices.Length; t += 3)
            {
                Vector3 a = positions[indices[t]], b = positions[indices[t + 1]], c = positions[indices[t + 2]];
                float area = ((b.X - a.X) * (c.Z - a.Z)) - ((c.X - a.X) * (b.Z - a.Z));
                if (MathF.Abs(area) < 1e-6f) continue;
                float u = (((b.X - x) * (c.Z - z)) - ((c.X - x) * (b.Z - z))) / area;
                float v = (((c.X - x) * (a.Z - z)) - ((a.X - x) * (c.Z - z))) / area;
                float w = 1f - u - v;
                if (u < -1e-5f || v < -1e-5f || w < -1e-5f) continue;
                float y = (u * a.Y) + (v * b.Y) + (w * c.Y);
                bool facing = shell.Normals[indices[t]].Y > 1e-3f;
                if (y > top + 1e-3f) (top, up) = (y, facing);
                else if (y > top - 1e-3f) up |= facing;
            }
        }

        return float.IsNegativeInfinity(top) || up;
    }

    private static float Top(FamilyBodyMesh mesh, string part) => mesh.Parts.Single(p => p.Part == part).Mesh.Positions.ToArray().Max(p => p.Y);

    private static (float Frontage, float Depth) Lot(string form) => form.StartsWith("deep", StringComparison.Ordinal) ? (40f, 34f) : (40f, 24f);

    private static (float, float, int)[] Lots(string form)
    {
        int most = form.EndsWith("span", StringComparison.Ordinal) ? 2 : 3;
        return form.StartsWith("deep", StringComparison.Ordinal)
            ? [(12f, 32f, 1), (40f, 34f, 2), (84f, 36f, most)]
            : [(12f, 8f, 1), (12f, 12f, 2), (20f, 16f, most), (36f, 24f, 2), (84f, 28f, most)];
    }

    private static HouseVariant Variant(FamilyBody body) => body.House!.Variant;

    private static BuildingFacts Facts(ulong id, ulong face, int offset, int frontage = 36, int depth = 20, int storeys = 2) =>
        new(id, "dwelling", frontage, depth, storeys, BlockPattern.Detached, LotTable.Housing, 0, face, StreetSide.Left,
            Offset: offset, ParcelFrontage: frontage);

    private static StylePreset TestStreet()
    {
        StylePresetResult read = StylePresetReader.Read(Path.Combine(AppContext.BaseDirectory, "Appearance", "test-street"));
        Assert.Empty(read.Errors);
        return read.Preset!;
    }

    private static FamilyBody House(string variant, string extra = "")
    {
        StylePresetResult read = ReadBody($"shape = \"house\"\nvariant = \"{variant}\"\n{extra}");
        Assert.Empty(read.Errors);
        return read.Preset!.Families[0].Body!;
    }

    private static string Describe(FamilyBodyMesh mesh) => string.Join(";", mesh.Parts.Select(p =>
    {
        float sum = 0f;
        foreach (Vector3 v in p.Mesh.Positions) sum += v.X + (3f * v.Y) + (7f * v.Z);
        foreach (Vector4 c in p.Mesh.Colors) sum += c.X + c.Y + c.Z;
        return $"{p.Part}:{p.Mesh.VertexCount}:{sum:F2}";
    }));

    private static StylePresetResult ReadBody(string body) => StylePresetReader.Read([("house.toml", $$"""
        [preset]
        name = "house"
        era_days = 1
        [[family]]
        id = "house"
        kinds = ["dwelling"]
        frontage_metres = [24, 64]
        [family.body]
        library = "library"
        {{body}}
        """)]);
}
