using System.Numerics;
using Borough.Appearance;

namespace Borough.Tests.Appearance;

public sealed class FamilyBodyTests
{
    private static FamilyBody OfficeWarehouse() => TestStreet("office-warehouse");

    private static FamilyBody TestStreet(string family)
    {
        StylePresetResult read = StylePresetReader.Read(Path.Combine(AppContext.BaseDirectory, "Appearance", "test-street"));
        Assert.Empty(read.Errors);
        return read.Preset!.Families.Single(f => f.Id == family).Body!;
    }

    private static Vector3[] Positions(FamilyBodyMesh mesh) => [.. mesh.Parts.SelectMany(p => p.Mesh.Positions.ToArray())];

    [Fact]
    public void Corridor_apartments_at_24_by_16_m_have_the_blender_bodys_bounds()
    {
        Vector3[] all = Positions(FamilyBodyBuilder.Build(TestStreet("corridor-apartments"), 24f, 16f, 3));

        Assert.Equal(-12.5f, all.Min(p => p.X), 3);
        Assert.Equal(12.5f, all.Max(p => p.X), 3);
        Assert.Equal(0f, all.Min(p => p.Y), 3);
        Assert.Equal(11.4f, all.Max(p => p.Y), 3);
        Assert.Equal(-8.06f, all.Min(p => p.Z), 3);
        Assert.Equal(9.2f, all.Max(p => p.Z), 3);
    }

    [Theory]
    [InlineData(7, "WWHHHWW")]
    [InlineData(8, "WWWHHWWW")]
    [InlineData(9, "WWWHHHWWW")]
    public void Bays_that_do_not_share_evenly_keep_a_symmetric_row_symmetric(int bays, string expected)
    {
        var row = new BayRow([BayKind.Window, BayKind.Hall, BayKind.Window], [true, true, true]);

        Assert.Equal(expected, string.Concat(row.Over(bays).Select(k => k == BayKind.Hall ? 'H' : 'W')));
    }

    [Theory]
    [InlineData(20f)]
    [InlineData(24f)]
    [InlineData(28f)]
    public void Neighbouring_hall_bays_share_one_door_centred_on_them(float frontage)
    {
        FamilyBodyMesh mesh = FamilyBodyBuilder.Build(TestStreet("corridor-apartments"), frontage, 16f, 3);
        Vector3[] doors = [.. mesh.Parts.Single(p => p.Part == "door").Mesh.Positions.ToArray()];
        Vector3[] street = [.. doors.Where(p => p.Z > 7f)];

        Assert.Equal(-1.2f, street.Min(p => p.X), 3);
        Assert.Equal(1.2f, street.Max(p => p.X), 3);
    }

    [Fact]
    public void Four_attached_six_metre_houses_have_the_blender_rows_bounds()
    {
        FamilyBody body = TestStreet("rowhouses");
        (float Offset, AttachedSides Sides)[] row =
        [
            (-9f, AttachedSides.Right), (-3f, AttachedSides.Left | AttachedSides.Right),
            (3f, AttachedSides.Left | AttachedSides.Right), (9f, AttachedSides.Left),
        ];
        Vector3[] all = [.. row.SelectMany(h => Positions(FamilyBodyBuilder.Build(body, 6f, 12f, 2, h.Sides)).Select(p => p + new Vector3(h.Offset, 0f, 0f)))];

        Assert.Equal(-12.2f, all.Min(p => p.X), 3);
        Assert.Equal(12.2f, all.Max(p => p.X), 3);
        Assert.Equal(0f, all.Min(p => p.Y), 3);
        Assert.Equal(10.05f, all.Max(p => p.Y), 2);
        Assert.Equal(-6.4f, all.Min(p => p.Z), 3);
        Assert.Equal(7f, all.Max(p => p.Z), 3);
    }

    [Fact]
    public void A_house_between_two_neighbours_stops_at_its_party_walls_and_shows_them_no_window()
    {
        FamilyBodyMesh mesh = FamilyBodyBuilder.Build(TestStreet("rowhouses"), 6f, 12f, 2, AttachedSides.Left | AttachedSides.Right);
        Vector3[] all = Positions(mesh);
        Vector3[] glass = [.. mesh.Parts.Single(p => p.Part == "glass").Mesh.Positions.ToArray()];

        Assert.Equal(-3f, all.Min(p => p.X), 3);
        Assert.Equal(3f, all.Max(p => p.X), 3);
        Assert.DoesNotContain(glass, p => MathF.Abs(p.X) > 2.5f && MathF.Abs(p.Z) < 5f);
    }

    [Fact]
    public void A_house_against_a_crosswise_neighbour_hips_its_roof_down_to_it()
    {
        FamilyBodyMesh mesh = FamilyBodyBuilder.Build(TestStreet("rowhouses"), 8f, 12f, 2,
            AttachedSides.Left | AttachedSides.LeftCrosswise | AttachedSides.Right);
        Vector3[] all = Positions(mesh);

        Assert.Equal(-4.4f, all.Min(p => p.X), 3);
        Assert.Equal(4f, all.Max(p => p.X), 3);
        Assert.DoesNotContain(all, p => p.X < -3.9f && p.Y > 7.1f);
    }

    [Fact]
    public void A_shallow_gable_and_a_chimney_on_a_flat_roof_are_refused()
    {
        StylePresetResult read = StylePresetReader.Read([("x.toml", """
            [preset]
            name = "x"
            era_days = 1

            [[family]]
            id = "a"
            kinds = ["dwelling"]

            [family.body]
            library = "l"
            bay_metres = 3
            gable_degrees = 12

            [[family]]
            id = "b"
            kinds = ["dwelling"]

            [family.body]
            library = "l"
            bay_metres = 3
            chimney = true
            """)]);

        Assert.Null(read.Preset);
        Assert.Contains(read.Errors, e => e.Message.Contains("'gable_degrees' must be a number of at least 18 degrees", StringComparison.Ordinal));
        Assert.Contains(read.Errors, e => e.Message.Contains("'chimney' needs 'gable_degrees'", StringComparison.Ordinal));
    }

    [Fact]
    public void An_opening_size_for_a_bay_that_fills_its_bay_is_refused()
    {
        StylePresetResult read = StylePresetReader.Read([("x.toml", """
            [preset]
            name = "x"
            era_days = 1

            [[family]]
            id = "a"
            kinds = ["dwelling"]

            [family.body]
            library = "l"
            bay_metres = 3
            openings = { shop = [2, 2], window = [1.6], door = [1, 2, -1] }
            """)]);

        Assert.Null(read.Preset);
        Assert.Contains(read.Errors, e => e.Message.Contains("'openings.shop' names no sized opening", StringComparison.Ordinal));
        Assert.Contains(read.Errors, e => e.Message.Contains("'openings.window' must be", StringComparison.Ordinal));
        Assert.Contains(read.Errors, e => e.Message.Contains("'openings.door' must be", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(6, new[] { BayKind.Shop, BayKind.Shop, BayKind.Entry, BayKind.Shop, BayKind.Shop, BayKind.Shop })]
    [InlineData(4, new[] { BayKind.Shop, BayKind.Shop, BayKind.Entry, BayKind.Shop })]
    [InlineData(3, new[] { BayKind.Shop, BayKind.Shop, BayKind.Entry })]
    public void A_filling_token_takes_up_the_bays_the_fixed_ones_leave(int bays, BayKind[] expected) =>
        Assert.Equal(expected, OfficeWarehouse().Street.Ground.Over(bays));

    [Fact]
    public void Two_filling_tokens_share_the_spare_bays_from_the_left()
    {
        var row = new BayRow([BayKind.Shop, BayKind.Entry, BayKind.Window], [true, false, true]);

        Assert.Equal([BayKind.Shop, BayKind.Shop, BayKind.Shop, BayKind.Entry, BayKind.Window, BayKind.Window], row.Over(6));
    }

    [Theory]
    [InlineData(36f, 20f)]
    [InlineData(32f, 16f)]
    [InlineData(40f, 24f)]
    public void The_body_fills_its_footprint_and_nothing_hangs_off_the_street_wall(float frontage, float depth)
    {
        FamilyBodyMesh mesh = FamilyBodyBuilder.Build(OfficeWarehouse(), frontage, depth, 2);
        Vector3[] all = [.. mesh.Parts.SelectMany(p => p.Mesh.Positions.ToArray())];

        Assert.Equal(-frontage / 2f - .2f, all.Min(p => p.X), 3);
        Assert.Equal(frontage / 2f + .2f, all.Max(p => p.X), 3);
        Assert.Equal(0f, all.Min(p => p.Y), 3);
        Assert.Equal(7f + 1.5f, all.Max(p => p.Y), 3);
        Assert.Equal(depth / 2f + 1.4f, all.Max(p => p.Z), 3);
        Assert.Contains(mesh.Parts, p => p.Part == "glass");
        Assert.Contains(mesh.Parts, p => p.Part == "door");
    }

    [Theory]
    [InlineData("office-warehouse", 36f, 20f, 2, AttachedSides.None)]
    [InlineData("corridor-apartments", 24f, 16f, 3, AttachedSides.None)]
    [InlineData("rowhouses", 6f, 12f, 3, AttachedSides.Left)]
    [InlineData("rowhouses", 8f, 12f, 3, AttachedSides.Right | AttachedSides.RightCrosswise)]
    [InlineData("rowhouses", 8f, 12f, 3, AttachedSides.Left | AttachedSides.LeftCrosswise | AttachedSides.Right | AttachedSides.RightCrosswise)]
    public void Every_triangle_faces_the_way_its_normal_says(string family, float frontage, float depth, int storeys, AttachedSides attached)
    {
        foreach ((_, ShellMesh part) in FamilyBodyBuilder.Build(TestStreet(family), frontage, depth, storeys, attached).Parts)
        {
            ReadOnlySpan<Vector3> p = part.Positions;
            ReadOnlySpan<int> i = part.Indices;
            for (int t = 0; t < i.Length; t += 3)
            {
                Vector3 winding = Vector3.Cross(p[i[t + 2]] - p[i[t]], p[i[t + 1]] - p[i[t]]);
                Assert.True(Vector3.Dot(winding, part.Normals[i[t]]) > 0f);
            }
        }
    }

    [Fact]
    public void A_body_under_a_fallback_is_refused()
    {
        StylePresetResult read = StylePresetReader.Read([("x.toml", """
            [preset]
            name = "x"
            era_days = 1

            [[family]]
            id = "box"
            fallback = true
            kinds = ["dwelling"]

            [family.body]
            library = "l"
            bay_metres = 6
            street_ground = ["arch"]
            """)]);

        Assert.Null(read.Preset);
        Assert.Contains(read.Errors, e => e.Message.Contains("'arch' is not a bay", StringComparison.Ordinal));
        Assert.Contains(read.Errors, e => e.Message.Contains("takes no body", StringComparison.Ordinal));
    }
}
