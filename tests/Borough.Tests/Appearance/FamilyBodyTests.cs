using System.Numerics;
using Borough.Appearance;

namespace Borough.Tests.Appearance;

public sealed class FamilyBodyTests
{
    private static FamilyBody W1()
    {
        StylePresetResult read = StylePresetReader.Read(Path.Combine(AppContext.BaseDirectory, "Appearance", "test-street"));
        Assert.Empty(read.Errors);
        return read.Preset!.Families.Single(f => f.Id == "w1-workplace").Body!;
    }

    [Theory]
    [InlineData(6, new[] { BayKind.Shop, BayKind.Shop, BayKind.Entry, BayKind.Shop, BayKind.Shop, BayKind.Shop })]
    [InlineData(4, new[] { BayKind.Shop, BayKind.Shop, BayKind.Entry, BayKind.Shop })]
    [InlineData(3, new[] { BayKind.Shop, BayKind.Shop, BayKind.Entry })]
    public void A_filling_token_takes_up_the_bays_the_fixed_ones_leave(int bays, BayKind[] expected) =>
        Assert.Equal(expected, W1().Street.Ground.Over(bays));

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
        FamilyBodyMesh mesh = FamilyBodyBuilder.Build(W1(), frontage, depth, 2);
        Vector3[] all = [.. mesh.Parts.SelectMany(p => p.Mesh.Positions.ToArray())];

        Assert.Equal(-frontage / 2f - .2f, all.Min(p => p.X), 3);
        Assert.Equal(frontage / 2f + .2f, all.Max(p => p.X), 3);
        Assert.Equal(0f, all.Min(p => p.Y), 3);
        Assert.Equal(7f + 1.5f, all.Max(p => p.Y), 3);
        Assert.Equal(depth / 2f + 1.4f, all.Max(p => p.Z), 3);
        Assert.Contains(mesh.Parts, p => p.Part == "glass");
        Assert.Contains(mesh.Parts, p => p.Part == "door");
    }

    [Fact]
    public void Every_triangle_faces_the_way_its_normal_says()
    {
        foreach ((_, ShellMesh part) in FamilyBodyBuilder.Build(W1(), 36f, 20f, 2).Parts)
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
