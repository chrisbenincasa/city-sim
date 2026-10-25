using System.Numerics;
using Borough.Appearance;

namespace Borough.Tests.Appearance;

/// <summary>The attached house forms of the test-street rowhouse families.</summary>
public sealed class TerraceBodyTests
{
    private static readonly string[] Forms =
    [
        "rowhouses", "rowhouses-parapet", "rowhouses-mansard", "rowhouses-front-gable", "rowhouses-paired-gable", "rowhouses-bay-front",
    ];

    private static readonly AttachedSides[] Sides =
    [
        AttachedSides.None, AttachedSides.Left, AttachedSides.Right, AttachedSides.Left | AttachedSides.Right,
    ];

    private static FamilyBody TestStreet(string family)
    {
        StylePresetResult read = StylePresetReader.Read(Path.Combine(AppContext.BaseDirectory, "Appearance", "test-street"));
        Assert.Empty(read.Errors);
        return read.Preset!.Families.Single(f => f.Id == family).Body!;
    }

    public static TheoryData<string, int, int, int> Shapes()
    {
        var data = new TheoryData<string, int, int, int>();
        foreach (string form in Forms)
        {
            foreach (int frontage in form == "rowhouses-paired-gable" ? (int[])[4, 8, 11] : [4, 8, 12])
            {
                foreach (int depth in (int[])[8, 12, 16])
                {
                    foreach (int storeys in (int[])[2, 3]) data.Add(form, frontage, depth, storeys);
                }
            }
        }

        return data;
    }

    private static IEnumerable<(AttachedSides Sides, TerracePlace Place, FamilyBodyMesh Mesh)> Houses(string form, int frontage, int depth, int storeys)
    {
        FamilyBody body = TestStreet(form);
        (int low, int high) = body.Terrace!.PitchDegrees is { } pitch ? ((int)pitch.Low, (int)pitch.High) : (0, 0);
        foreach (AttachedSides sides in Sides)
        {
            foreach (TerracePlace place in (TerracePlace[])[new(low, 0, 4), new(high, 1, 4), new(high, 3, 4)])
            {
                yield return (sides, place, FamilyBodyBuilder.Build(body, frontage, depth, storeys, sides, place));
            }
        }
    }

    [Theory]
    [MemberData(nameof(Shapes))]
    public void Every_triangle_faces_the_way_its_normal_says(string form, int frontage, int depth, int storeys)
    {
        foreach ((_, _, FamilyBodyMesh mesh) in Houses(form, frontage, depth, storeys))
        {
            foreach ((_, ShellMesh part) in mesh.Parts)
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
    }

    /// <remarks>A chimney stack straddles the party wall it stands on, and nothing else crosses one.</remarks>
    [Theory]
    [MemberData(nameof(Shapes))]
    public void A_house_stays_on_its_Lot_and_stops_at_its_party_walls(string form, int frontage, int depth, int storeys)
    {
        float x = frontage / 2f, y = depth / 2f;
        foreach ((AttachedSides sides, _, FamilyBodyMesh mesh) in Houses(form, frontage, depth, storeys))
        {
            foreach ((string part, ShellMesh shell) in mesh.Parts)
            {
                float stack = part.StartsWith("wall-end", StringComparison.Ordinal) ? .4f : 0f;
                float left = sides.HasFlag(AttachedSides.Left) ? x + stack : x + .5f;
                float right = sides.HasFlag(AttachedSides.Right) ? x + stack : x + .5f;
                foreach (Vector3 p in shell.Positions)
                {
                    Assert.InRange(p.X, -left - 1e-3f, right + 1e-3f);
                    Assert.InRange(p.Z, -y - .5f, y + 1f);
                    Assert.InRange(p.Y, 0f, (storeys * ShellBuilder.StoreyMetres) + 8f);
                }
            }
        }
    }

    [Fact]
    public void A_pair_shares_one_gable_whose_ridge_stands_on_their_shared_wall()
    {
        FamilyBody body = TestStreet("rowhouses-paired-gable");
        const AttachedSides Both = AttachedSides.Left | AttachedSides.Right;
        Vector3 left = Ridge(FamilyBodyBuilder.Build(body, 6f, 12f, 2, Both, new TerracePlace(30, 2, 4)));
        Vector3 right = Ridge(FamilyBodyBuilder.Build(body, 6f, 12f, 2, Both, new TerracePlace(30, 3, 4)));
        Vector3 alone = Ridge(FamilyBodyBuilder.Build(body, 6f, 12f, 2, AttachedSides.Left, new TerracePlace(30, 4, 5)));

        Assert.Equal(3f, left.X, 3);
        Assert.Equal(-3f, right.X, 3);
        Assert.Equal(left.Y, right.Y, 3);
        Assert.Equal(0f, alone.X, 3);
        Assert.True(alone.Y < left.Y);
    }

    private static Vector3 Ridge(FamilyBodyMesh mesh) =>
        mesh.Parts.Single(p => p.Part == "roof").Mesh.Positions.ToArray().MaxBy(p => p.Y);

    [Fact]
    public void A_one_bay_bay_front_keeps_its_wall_on_the_line_under_an_oriel()
    {
        FamilyBody body = TestStreet("rowhouses-bay-front");
        Vector3[] narrow = [.. FamilyBodyBuilder.Build(body, 4f, 12f, 3, AttachedSides.Left | AttachedSides.Right, new TerracePlace(30, 0, 4))
            .Parts.Single(p => p.Part == "glass").Mesh.Positions.ToArray()];
        Vector3[] wide = [.. FamilyBodyBuilder.Build(body, 8f, 12f, 3, AttachedSides.Left | AttachedSides.Right, new TerracePlace(30, 0, 4))
            .Parts.Single(p => p.Part == "glass").Mesh.Positions.ToArray()];

        Assert.Contains(narrow, p => p.Z > 6.4f && p.Y > ShellBuilder.StoreyMetres);
        Assert.DoesNotContain(narrow, p => p.Z > 6.1f && p.Y < ShellBuilder.StoreyMetres);
        Assert.Contains(wide, p => p.Z > 5.9f && p.Y < ShellBuilder.StoreyMetres);
        Assert.Contains(wide, p => p.Z is > 4.8f and < 5.2f);
    }
}
