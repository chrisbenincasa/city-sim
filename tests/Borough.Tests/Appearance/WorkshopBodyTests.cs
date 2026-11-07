using System.Numerics;
using Borough.Appearance;

namespace Borough.Tests.Appearance;

/// <summary>The dressed workshop forms of the test-street workshop families.</summary>
public sealed class WorkshopBodyTests
{
    private static readonly AttachedSides[] Sides =
    [
        AttachedSides.None, AttachedSides.Left, AttachedSides.Right, AttachedSides.Left | AttachedSides.Right,
    ];

    private static readonly Lazy<AppearanceFamily[]> Families = new(() =>
    {
        StylePresetResult read = StylePresetReader.Read(Path.Combine(AppContext.BaseDirectory, "Appearance", "test-street"));
        Assert.Empty(read.Errors);
        return [.. read.Preset!.Families.Where(f => f.Body?.Workshop is not null)];
    });

    private static AppearanceFamily Family(string id) => Families.Value.Single(f => f.Id == id);

    public static TheoryData<string> Forms()
    {
        var data = new TheoryData<string>();
        foreach (AppearanceFamily family in Families.Value)
        {
            if (family.Body!.Workshop!.Form != WorkshopForm.PanelShed) data.Add(family.Id);
        }

        return data;
    }

    private static IEnumerable<(int Frontage, int Depth, int Storeys, AttachedSides Sides, int Seed, FamilyBodyMesh Mesh)> Bodies(string id)
    {
        FamilyBody body = Family(id).Body!;
        int pitch = body.Workshop!.PitchDegrees is { } range ? (int)range.High : 0;
        foreach (int frontage in (int[])[12, 20, 28, 36])
        {
            foreach (int depth in (int[])[8, 12, 20])
            {
                foreach (int storeys in (int[])[1, 2, 3])
                {
                    foreach (AttachedSides sides in Sides)
                    {
                        for (int k = 0; k < 3; k++)
                        {
                            int seed = (frontage * 7) + depth + storeys + (k * 101);
                            yield return (frontage, depth, storeys, sides, seed,
                                FamilyBodyBuilder.BuildWorkshop(body, frontage, depth, storeys, sides, seed, seed * 3, new TerracePlace(pitch, 0, 2)));
                        }
                    }
                }
            }
        }
    }

    [Fact]
    public void The_test_street_draws_five_dressed_forms_and_keeps_the_panel_shed()
    {
        Assert.Equal(5, Families.Value.Count(f => f.Body!.Workshop!.Form != WorkshopForm.PanelShed));
        Assert.Equal(WorkshopForm.PanelShed, Family("workshop").Body!.Workshop!.Form);
    }

    [Theory]
    [MemberData(nameof(Forms))]
    public void Every_triangle_faces_the_way_its_normal_says(string family)
    {
        var wrong = new List<string>();
        foreach ((int frontage, int depth, int storeys, AttachedSides sides, int seed, FamilyBodyMesh mesh) in Bodies(family))
        {
            foreach ((string name, ShellMesh part) in mesh.Parts)
            {
                ReadOnlySpan<Vector3> p = part.Positions;
                ReadOnlySpan<int> i = part.Indices;
                for (int t = 0; t < i.Length; t += 3)
                {
                    Vector3 winding = Vector3.Cross(p[i[t + 2]] - p[i[t]], p[i[t + 1]] - p[i[t]]);
                    if (Vector3.Dot(winding, part.Normals[i[t]]) <= 0f) wrong.Add($"{frontage}x{depth} {storeys} {sides} seed {seed}: {name} at {p[i[t]]} {p[i[t + 1]]} {p[i[t + 2]]}");
                }
            }
        }

        Assert.True(wrong.Count == 0, string.Join('\n', wrong.Take(10)));
    }

    /// <remarks>
    /// Canopies and the builder's yard hoist reach under 2 m over the pavement, and a free side's verge
    /// or gutter about half a metre over the side street. A works chimney tops the eaves by about 9 m.
    /// </remarks>
    [Theory]
    [MemberData(nameof(Forms))]
    public void A_body_stays_on_its_Lot_and_stops_at_its_party_walls(string family)
    {
        var outside = new List<string>();
        foreach ((int frontage, int depth, int storeys, AttachedSides sides, int seed, FamilyBodyMesh mesh) in Bodies(family))
        {
            float x = frontage / 2f, y = depth / 2f;
            float left = sides.HasFlag(AttachedSides.Left) ? x : x + 1f;
            float right = sides.HasFlag(AttachedSides.Right) ? x : x + 1f;
            foreach ((string part, ShellMesh shell) in mesh.Parts)
            {
                foreach (Vector3 p in shell.Positions)
                {
                    if (p.X < -left - 1e-3f || p.X > right + 1e-3f || p.Z < -y - 1.1f || p.Z > y + 2.1f
                        || p.Y < -1e-3f || p.Y > (storeys * ShellBuilder.StoreyMetres) + 12f)
                    {
                        outside.Add($"{family} {frontage}x{depth} {storeys} {sides} seed {seed}: {part} at {p}");
                        break;
                    }
                }
            }
        }

        Assert.True(outside.Count == 0, string.Join('\n', outside.Take(40)));
    }

    [Theory]
    [MemberData(nameof(Forms))]
    public void A_roof_faces_the_sky(string family)
    {
        foreach ((_, _, _, _, _, FamilyBodyMesh mesh) in Bodies(family))
        {
            foreach ((string part, ShellMesh roof) in mesh.Parts)
            {
                if (!part.StartsWith("roof", StringComparison.Ordinal)) continue;
                foreach (Vector3 normal in roof.Normals) Assert.True(normal.Y > .2f);
            }
        }
    }

    [Theory]
    [MemberData(nameof(Forms))]
    public void A_run_shares_its_walls_and_each_workshop_draws_its_own_doors_and_sign(string id)
    {
        FamilyBody body = Family(id).Body!;
        var walls = new HashSet<string>(StringComparer.Ordinal);
        var own = new HashSet<string>(StringComparer.Ordinal);
        for (int seed = 0; seed < 16; seed++)
        {
            FamilyBodyMesh mesh = FamilyBodyBuilder.BuildWorkshop(body, 32f, 16f, 2, AttachedSides.Left | AttachedSides.Right, seed, 5, new TerracePlace(20, 0, 3));
            walls.Add(string.Join(' ', mesh.Parts.Where(p => p.Part.StartsWith("wall", StringComparison.Ordinal)).Select(p => p.Part)));
            own.Add(string.Join(' ', mesh.Parts.Where(p => p.Part is "door" or "trim").Select(p => p.Part + Paints(p.Mesh)))
                + Signature(mesh));
        }

        Assert.True(walls.Count == 1, string.Join('\n', walls));
        Assert.True(own.Count >= 8, string.Join('\n', own));
    }

    private static string Paints(ShellMesh mesh) =>
        string.Join(',', mesh.Colors.ToArray().Distinct().Order(Comparer<Vector4>.Create((a, b) => (a.X, a.Y, a.Z).CompareTo((b.X, b.Y, b.Z)))));

    /// <summary>The Building's layout, read from where its glass stands.</summary>
    private static string Signature(FamilyBodyMesh mesh) =>
        string.Join(',', mesh.Parts.Where(p => p.Part == "glass").SelectMany(p => p.Mesh.Positions.ToArray()).Select(p => MathF.Round(p.X)).Distinct().Order());
}
