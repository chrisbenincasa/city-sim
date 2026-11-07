using System.Numerics;
using Borough.Appearance;

namespace Borough.Tests.Appearance;

/// <summary>The apartment forms of the test-street apartment families.</summary>
public sealed class ApartmentBodyTests
{
    private static readonly AttachedSides[] Sides =
    [
        AttachedSides.None, AttachedSides.Left, AttachedSides.Right, AttachedSides.Left | AttachedSides.Right,
    ];

    private static readonly Lazy<AppearanceFamily[]> Families = new(() =>
    {
        StylePresetResult read = StylePresetReader.Read(Path.Combine(AppContext.BaseDirectory, "Appearance", "test-street"));
        Assert.Empty(read.Errors);
        return [.. read.Preset!.Families.Where(f => f.Body?.Apartment is not null)];
    });

    private static AppearanceFamily Family(string id) => Families.Value.Single(f => f.Id == id);

    public static TheoryData<string, int, int, int> Shapes()
    {
        var data = new TheoryData<string, int, int, int>();
        foreach (AppearanceFamily family in Families.Value)
        {
            (long low, long high) = (family.FrontageMetres!.Value.Low, family.FrontageMetres.Value.High);
            foreach (long frontage in (long[])[low, (low + high) / 2, high])
            {
                foreach (long depth in (long[])[family.DepthMetres!.Value.Low, family.DepthMetres.Value.High])
                {
                    data.Add(family.Id, (int)frontage, (int)depth, (int)family.Storeys!.Value.High);
                }
            }
        }

        return data;
    }

    private static IEnumerable<(AttachedSides Sides, FamilyBodyMesh Mesh)> Bodies(string id, int frontage, int depth, int storeys)
    {
        AppearanceFamily family = Family(id);
        FamilyBody body = family.Body!;
        bool detached = family.Patterns is { } patterns && Array.IndexOf(patterns, Borough.Core.Space.BlockPattern.Detached) >= 0;
        (int low, int high) = body.Apartment!.PitchDegrees is { } pitch ? ((int)pitch.Low, (int)pitch.High) : (0, 0);
        foreach (AttachedSides sides in detached ? [AttachedSides.None] : Sides)
        {
            foreach (int degrees in (int[])[low, high])
            {
                yield return (sides, FamilyBodyBuilder.Build(body, frontage, depth, storeys, sides, new TerracePlace(degrees, 0, 2)));
            }
        }
    }

    [Theory]
    [MemberData(nameof(Shapes))]
    public void Every_triangle_faces_the_way_its_normal_says(string family, int frontage, int depth, int storeys)
    {
        foreach ((_, FamilyBodyMesh mesh) in Bodies(family, frontage, depth, storeys))
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

    /// <remarks>
    /// Street canopies stand 1.2 m before the street wall and balconies 1.4 m behind the back wall.
    /// The parapet form is the base body, whose coping overhangs every wall by 5 cm.
    /// </remarks>
    [Theory]
    [MemberData(nameof(Shapes))]
    public void A_body_stays_on_its_Lot_and_stops_at_its_party_walls(string family, int frontage, int depth, int storeys)
    {
        float x = frontage / 2f, y = depth / 2f;
        float coping = Family(family).Body!.Apartment!.Form == ApartmentForm.Parapet ? .05f : 0f;
        foreach ((AttachedSides sides, FamilyBodyMesh mesh) in Bodies(family, frontage, depth, storeys))
        {
            float left = sides.HasFlag(AttachedSides.Left) ? x + coping : x + .5f;
            float right = sides.HasFlag(AttachedSides.Right) ? x + coping : x + .5f;
            foreach ((_, ShellMesh shell) in mesh.Parts)
            {
                foreach (Vector3 p in shell.Positions)
                {
                    Assert.InRange(p.X, -left - 1e-3f, right + 1e-3f);
                    Assert.InRange(p.Z, -y - 1.5f, y + 1.3f);
                    Assert.InRange(p.Y, 0f, (storeys * ShellBuilder.StoreyMetres) + 15f);
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(Shapes))]
    public void A_pitched_roof_faces_the_sky(string family, int frontage, int depth, int storeys)
    {
        if (Family(family).Body!.Apartment!.PitchDegrees is null) return;
        foreach ((_, FamilyBodyMesh mesh) in Bodies(family, frontage, depth, storeys))
        {
            ShellMesh roof = mesh.Parts.Single(p => p.Part == "roof").Mesh;
            foreach (Vector3 normal in roof.Normals) Assert.True(normal.Y > .2f);
        }
    }

    [Fact]
    public void A_hipped_roof_deeper_than_it_is_wide_turns_its_ridge_back_from_the_street()
    {
        FamilyBody body = Family("detached-two-unit-hipped").Body!;
        Vector3[] deep = Ridge(FamilyBodyBuilder.Build(body, 16f, 26f, 4, AttachedSides.None, new TerracePlace(26, 0, 1)));
        Vector3[] wide = Ridge(FamilyBodyBuilder.Build(body, 28f, 16f, 4, AttachedSides.None, new TerracePlace(26, 0, 1)));

        Assert.True(deep.Max(p => p.Z) - deep.Min(p => p.Z) > 9f);
        Assert.Equal(0f, deep.Max(p => p.X) - deep.Min(p => p.X), 3);
        Assert.True(wide.Max(p => p.X) - wide.Min(p => p.X) > 11f);
        Assert.Equal(0f, wide.Max(p => p.Z) - wide.Min(p => p.Z), 3);
    }

    private static Vector3[] Ridge(FamilyBodyMesh mesh)
    {
        Vector3[] roof = mesh.Parts.Single(p => p.Part == "roof").Mesh.Positions.ToArray();
        float top = roof.Max(p => p.Y);
        return [.. roof.Where(p => p.Y > top - 1e-3f)];
    }

    [Theory]
    [InlineData("corridor-apartments-mansard")]
    [InlineData("walkup-apartments-setback")]
    public void A_mansard_and_a_set_back_top_hold_their_top_storey_off_the_street_wall(string id)
    {
        FamilyBodyMesh mesh = FamilyBodyBuilder.Build(Family(id).Body!, 28f, 16f, 3, AttachedSides.Left | AttachedSides.Right, default);
        float eaves = 2 * ShellBuilder.StoreyMetres;
        Vector3[] glass = mesh.Parts.Single(p => p.Part == "glass").Mesh.Positions.ToArray();

        Assert.Contains(glass, p => p.Y > eaves);
        Assert.DoesNotContain(glass, p => p.Y > eaves && p.Z > 8f - .2f);
        Assert.Contains(glass, p => p.Y < eaves && p.Z > 8f - .5f);
    }

    [Fact]
    public void A_forecourt_holds_its_middle_back_and_its_wings_on_the_line()
    {
        FamilyBodyMesh mesh = FamilyBodyBuilder.Build(Family("two-unit-apartments-forecourt").Body!, 24f, 16f, 3,
            AttachedSides.Left | AttachedSides.Right, default);
        Vector3[] glass = mesh.Parts.Single(p => p.Part == "glass").Mesh.Positions.ToArray();

        Assert.Contains(glass, p => Math.Abs(p.X) > 7f && p.Z > 7.5f);
        Assert.DoesNotContain(glass, p => Math.Abs(p.X) < 5f && p.Z > 8f - FamilyBodyBuilder.ForecourtMetres + .5f);
        Assert.Contains(glass, p => Math.Abs(p.X) < 5f && p.Z > 8f - FamilyBodyBuilder.ForecourtMetres - .5f);
    }
}
