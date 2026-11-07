using System.Numerics;
using Borough.Appearance;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Space;

namespace Borough.Tests.Appearance;

/// <summary>The detached store forms of the test-street shop families.</summary>
public sealed class DetachedShopBodyTests
{
    private static readonly WorldKey World = WorldKey.FromSeed(11);

    private static readonly Lazy<StylePreset> Preset = new(() =>
    {
        StylePresetResult read = StylePresetReader.Read(Path.Combine(AppContext.BaseDirectory, "Appearance", "test-street"));
        Assert.Empty(read.Errors);
        return read.Preset!;
    });

    private static readonly (float Frontage, float Depth, int Storeys)[] Lots =
        [(16f, 12f, 2), (20f, 20f, 2), (24f, 16f, 2), (44f, 20f, 2), (20f, 28f, 3), (32f, 28f, 3), (52f, 24f, 3), (60f, 16f, 3)];

    public static TheoryData<string> Forms =>
        ["glass-base", "gabled-hall", "colonnade", "mono-pitch", "canopy", "folded-plate", "commercial-block"];

    private static AppearanceFamily Family(string form) => Preset.Value.Families.Single(f => f.Id == $"detached-shop-{form}");

    private static FamilyBodyMesh Build(string form, float frontage, float depth, int storeys, int seed)
    {
        FamilyBody body = Family(form).Body!;
        int pitch = body.Shop!.PitchDegrees is { } range ? (int)range.High : 0;
        return FamilyBodyBuilder.BuildShop(body, frontage, depth, storeys, AttachedSides.None, false, seed, seed, new TerracePlace(pitch, 0, 2));
    }

    private static IEnumerable<(string Lot, float Frontage, float Depth, int Storeys, FamilyBodyMesh Mesh)> Bodies(string form)
    {
        foreach ((float frontage, float depth, int storeys) in Lots)
        {
            foreach (int seed in (int[])[3, 140])
            {
                yield return ($"{frontage}x{depth} {storeys} seed {seed}", frontage, depth, storeys, Build(form, frontage, depth, storeys, seed));
            }
        }
    }

    [Theory]
    [MemberData(nameof(Forms))]
    public void Every_triangle_faces_the_way_its_normal_says(string form)
    {
        var wrong = new List<string>();
        foreach ((string lot, _, _, _, FamilyBodyMesh mesh) in Bodies(form))
        {
            foreach ((string name, ShellMesh part) in mesh.Parts)
            {
                ReadOnlySpan<Vector3> p = part.Positions;
                ReadOnlySpan<int> i = part.Indices;
                for (int t = 0; t < i.Length; t += 3)
                {
                    Vector3 winding = Vector3.Cross(p[i[t + 2]] - p[i[t]], p[i[t + 1]] - p[i[t]]);
                    if (Vector3.Dot(winding, part.Normals[i[t]]) <= 0f) wrong.Add($"{lot}: {name} at {p[i[t]]}");
                }
            }
        }

        Assert.True(wrong.Count == 0, string.Join('\n', wrong.Take(10)));
    }

    /// <remarks>
    /// The canopy and its sign reach 2.85 m over the pavement and 2.8 m past each side; the mono-pitch
    /// oversails the street by 0.94 m. A roof oversails the back by at most 0.66 m. The name panel tops
    /// the parapet by 2 m, and a 45-degree gable rises half its width.
    /// </remarks>
    [Theory]
    [MemberData(nameof(Forms))]
    public void A_body_stays_near_its_footprint(string form)
    {
        var outside = new List<string>();
        foreach ((string lot, float frontage, float depth, int storeys, FamilyBodyMesh mesh) in Bodies(form))
        {
            float x = frontage / 2f, y = depth / 2f, top = (storeys * ShellBuilder.StoreyMetres) + MathF.Max(2.1f, (x / 2f) + .5f);
            foreach ((string part, ShellMesh shell) in mesh.Parts)
            {
                foreach (Vector3 p in shell.Positions)
                {
                    if (MathF.Abs(p.X) > x + 2.85f || p.Z < -y - .7f || p.Z > y + 2.9f || p.Y < -1e-3f || p.Y > top)
                    {
                        outside.Add($"{lot}: {part} at {p}");
                        break;
                    }
                }
            }
        }

        Assert.True(outside.Count == 0, string.Join('\n', outside.Take(20)));
    }

    [Theory]
    [MemberData(nameof(Forms))]
    public void The_top_surface_over_the_footprint_faces_the_sky(string form)
    {
        var down = new List<string>();
        foreach ((string lot, float frontage, float depth, _, FamilyBodyMesh mesh) in Bodies(form))
        {
            for (int i = 1; i < 8; i++)
            {
                for (int j = 1; j < 6; j++)
                {
                    float x = (-frontage / 2f) + (frontage * i / 8f) + .13f, z = (-depth / 2f) + (depth * j / 6f) + .07f;
                    if (!TopFacesUp(mesh, x, z)) down.Add($"{lot}: at ({x}, {z})");
                }
            }
        }

        Assert.True(down.Count == 0, string.Join('\n', down.Take(10)));
    }

    [Theory]
    [MemberData(nameof(Forms))]
    public void Each_Building_draws_its_own_look(string form)
    {
        Assert.Equal(Describe(Build(form, 32f, 20f, 2, 17)), Describe(Build(form, 32f, 20f, 2, 17)));

        int distinct = Enumerable.Range(0, 16).Select(seed => Describe(Build(form, 32f, 20f, 2, seed))).Distinct().Count();

        Assert.True(distinct >= 12, $"{form}: 16 seeds built {distinct} distinct bodies");
    }

    [Fact]
    public void Detached_shopfronts_draw_stores_at_the_period_shares_and_neighbors_differ_across_parades()
    {
        StylePreset preset = Preset.Value;
        var periods = new Dictionary<string, int>(StringComparer.Ordinal);
        int stores = 0, others = 0, pairs = 0;
        for (ulong face = 1; face <= 400; face++)
        {
            (ShopForm Form, Terrace Run)? before = null;
            for (int i = 0; i < 8; i++)
            {
                (float frontage, float depth, int storeys) = Lots[(int)((face + (ulong)i) % (ulong)Lots.Length)];
                var facts = new BuildingFacts((face * 100) + (ulong)i, "shopfront", (int)frontage, (int)depth, storeys, BlockPattern.Detached,
                    LotTable.Trade, 0, face, StreetSide.Left, Offset: (64 * i) + 1, ParcelFrontage: 64);
                FamilyPick pick = FamilyPicker.Pick(preset, World, facts);
                Assert.Equal(FamilyChoice.Eligible, pick.Choice);
                if (pick.Family!.Body?.Shop is not { } shop)
                {
                    others++;
                    before = null;
                    continue;
                }

                Assert.True(FamilyBodyBuilder.IsDetachedShop(shop.Form), pick.Family.Id);
                stores++;
                string period = shop.Form switch
                {
                    ShopForm.Canopy or ShopForm.FoldedPlate => "mid-century",
                    ShopForm.CommercialBlock => "traditional",
                    _ => "contemporary",
                };
                periods[period] = periods.GetValueOrDefault(period) + 1;
                Terrace run = FamilyPicker.ShopRunOf(preset, World, facts)!.Value;
                if (before is { } last)
                {
                    pairs++;
                    Assert.Equal(run.Cell == last.Run.Cell, shop.Form == last.Form);
                }

                before = (shop.Form, run);
            }
        }

        Assert.True(others * 10 < stores, $"{others} of {stores + others} detached shopfronts drew another family");
        Assert.True(pairs > 500, $"only {pairs} neighboring store pairs");
        Assert.InRange(periods["contemporary"] / (float)stores, .50f, .60f);
        Assert.InRange(periods["mid-century"] / (float)stores, .20f, .30f);
        Assert.InRange(periods["traditional"] / (float)stores, .15f, .25f);
    }

    [Theory]
    [InlineData("variant = \"gabled-hall\"", "within 5 to 45 degrees")]
    [InlineData("variant = \"mono-pitch\"\npitch_degrees = [2, 10]", "within 5 to 45 degrees")]
    [InlineData("variant = \"canopy\"\npitch_degrees = [10, 20]", "has no pitch")]
    public void A_detached_shop_body_refuses_a_pitch_its_form_does_not_take(string body, string message)
    {
        StylePresetResult read = StylePresetReader.Read([("shop.toml", $$"""
            [preset]
            name = "shop"
            era_days = 1
            [[family]]
            id = "shop"
            kinds = ["shopfront"]
            [family.body]
            library = "library"
            shape = "shop"
            {{body}}
            """)]);

        Assert.Null(read.Preset);
        Assert.Contains(read.Errors, e => e.Message.Contains(message, StringComparison.Ordinal));
    }

    [Fact]
    public void Dressing_files_every_shop_texture_at_its_tile_size()
    {
        var library = TextureLibrary.Read(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Appearance", "library", "materials.json")));

        StylePresetResult dressed = library.Dress(Preset.Value);

        Assert.Empty(dressed.Errors);
        FamilyBody body = dressed.Preset!.Families.Single(f => f.Id == "detached-shop-glass-base").Body!;
        Assert.All(FamilyBodyBuilder.ShopTextures, texture => Assert.Equal(library.TileMetres[texture], body.TileMetres[texture]));
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

    private static string Describe(FamilyBodyMesh mesh) => string.Join(";", mesh.Parts.Select(p =>
    {
        float sum = 0f;
        foreach (Vector3 v in p.Mesh.Positions) sum += v.X + (3f * v.Y) + (7f * v.Z);
        foreach (Vector4 c in p.Mesh.Colors) sum += c.X + c.Y + c.Z;
        return $"{p.Part}:{p.Mesh.VertexCount}:{sum:F2}";
    }));
}
