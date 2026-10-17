using System.Numerics;
using Borough.Appearance;

namespace Borough.Tests.Appearance;

public sealed class HouseBodyTests
{
    public static TheoryData<string> Variants => ["terrace", "front-gables", "villa"];

    [Theory]
    [MemberData(nameof(Variants))]
    public void The_same_seed_builds_the_same_house(string variant)
    {
        FamilyBody body = House(variant);

        Assert.Equal(Describe(FamilyBodyBuilder.BuildHouse(body, 40f, 24f, 2, 17)),
            Describe(FamilyBodyBuilder.BuildHouse(body, 40f, 24f, 2, 17)));
    }

    [Theory]
    [MemberData(nameof(Variants))]
    public void Different_seeds_build_different_houses_on_one_lot(string variant)
    {
        FamilyBody body = House(variant);

        int distinct = Enumerable.Range(0, 16).Select(seed => Describe(FamilyBodyBuilder.BuildHouse(body, 40f, 24f, 2, seed))).Distinct().Count();

        Assert.True(distinct >= 12, $"{variant}: 16 seeds built {distinct} distinct houses");
    }

    [Theory]
    [MemberData(nameof(Variants))]
    public void Every_draw_stays_on_its_lot_and_faces_the_way_it_is_wound(string variant)
    {
        FamilyBody body = House(variant);
        foreach ((float frontage, float depth, int storeys) in new[] { (24f, 12f, 1), (40f, 24f, 2), (64f, 28f, 3) })
        {
            for (int seed = 0; seed < FamilyBodyBuilder.HouseSeeds; seed += 7)
            {
                FamilyBodyMesh mesh = FamilyBodyBuilder.BuildHouse(body, frontage, depth, storeys, seed);
                foreach ((string part, ShellMesh shell) in mesh.Parts)
                {
                    ReadOnlySpan<Vector3> positions = shell.Positions;
                    for (int i = 0; i < positions.Length; i++)
                    {
                        Vector3 p = positions[i];
                        string at = $"{variant} {frontage}x{depth}x{storeys} seed {seed} {part}";
                        Assert.InRange(MathF.Abs(p.X), 0f, (frontage / 2f) + .5f);
                        Assert.InRange(MathF.Abs(p.Z), 0f, (depth / 2f) + .5f);
                        Assert.True(p.Y >= 0f && p.Y < (storeys * ShellBuilder.StoreyMetres) + 16f, at);
                    }

                    ReadOnlySpan<int> indices = shell.Indices;
                    for (int t = 0; t < indices.Length; t += 3)
                    {
                        Vector3 a = positions[indices[t]], b = positions[indices[t + 1]], c = positions[indices[t + 2]];
                        Vector3 winding = Vector3.Cross(b - a, c - a);
                        Assert.True(Vector3.Dot(winding, shell.Normals[indices[t]]) <= 1e-6f,
                            $"{variant} seed {seed} {part}: a triangle is wound against its normal");
                    }
                }
            }
        }
    }

    [Fact]
    public void A_house_paints_its_parts_through_vertex_colour()
    {
        FamilyBodyMesh mesh = FamilyBodyBuilder.BuildHouse(House("terrace"), 40f, 24f, 2, 3);

        ShellMesh door = mesh.Parts.Single(p => p.Part == "door").Mesh;
        ShellMesh glass = mesh.Parts.Single(p => p.Part == "glass").Mesh;

        Assert.All(door.Colors.ToArray(), c => Assert.Equal(1f, c.W));
        Assert.All(glass.Colors.ToArray(), c => Assert.Equal(0f, c.W));
        Assert.DoesNotContain(mesh.Parts, p => p.Part.Contains('#', StringComparison.Ordinal));
    }

    [Fact]
    public void A_villa_needs_frontage_for_its_bays()
    {
        FamilyBody villa = House("villa");

        Assert.Throws<ArgumentOutOfRangeException>(() => FamilyBodyBuilder.BuildHouse(villa, 20f, 24f, 2, 0));
        FamilyBodyBuilder.BuildHouse(villa, FamilyBodyBuilder.VillaLeastFrontage, 24f, 2, 0);
    }

    [Fact]
    public void Each_build_entry_point_refuses_a_house_or_a_block()
    {
        FamilyBody block = ReadBody("bay_metres = 4").Preset!.Families[0].Body!;
        FamilyBody house = House("terrace");

        Assert.Throws<ArgumentException>(() => FamilyBodyBuilder.Build(house, 40f, 24f, 2));
        Assert.Throws<ArgumentException>(() => FamilyBodyBuilder.BuildHouse(block, 40f, 24f, 2, 0));
    }

    [Fact]
    public void A_house_refuses_an_unknown_variant_and_block_keys()
    {
        StylePresetResult unknown = ReadBody("shape = \"house\"\nvariant = \"castle\"");
        StylePresetResult block = ReadBody("shape = \"house\"\nvariant = \"villa\"\nbay_metres = 4");

        Assert.Null(unknown.Preset);
        Assert.Contains(unknown.Errors, e => e.Message.Contains("\"terrace\", \"front-gables\" or \"villa\"", StringComparison.Ordinal));
        Assert.Null(block.Preset);
        Assert.Contains(block.Errors, e => e.Message.Contains("'bay_metres' is only valid for a block body", StringComparison.Ordinal));
    }

    [Fact]
    public void Dressing_files_every_house_texture_at_its_tile_size()
    {
        var library = TextureLibrary.Read(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Appearance", "library", "materials.json")));

        StylePresetResult dressed = library.Dress(ReadBody("shape = \"house\"\nvariant = \"villa\"").Preset!);

        FamilyBody body = dressed.Preset!.Families[0].Body!;
        Assert.All(FamilyBodyBuilder.HouseTextures, texture => Assert.Equal(library.TileMetres[texture], body.TileMetres[texture]));
    }

    private static FamilyBody House(string variant)
    {
        StylePresetResult read = ReadBody($"shape = \"house\"\nvariant = \"{variant}\"");
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
        [family.body]
        library = "library"
        {{body}}
        """)]);
}
