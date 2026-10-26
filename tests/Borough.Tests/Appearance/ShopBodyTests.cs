using System.Numerics;
using Borough.Appearance;

namespace Borough.Tests.Appearance;

/// <summary>The shop-house forms of the test-street shop families.</summary>
public sealed class ShopBodyTests
{
    private static readonly AttachedSides[] Sides =
    [
        AttachedSides.None, AttachedSides.Left, AttachedSides.Right, AttachedSides.Left | AttachedSides.Right,
    ];

    private static readonly Lazy<AppearanceFamily[]> Families = new(() =>
    {
        StylePresetResult read = StylePresetReader.Read(Path.Combine(AppContext.BaseDirectory, "Appearance", "test-street"));
        Assert.Empty(read.Errors);
        return [.. read.Preset!.Families.Where(f => f.Body?.Shop is not null)];
    });

    private static AppearanceFamily Family(string id) => Families.Value.Single(f => f.Id == id);

    public static TheoryData<string> Forms()
    {
        var data = new TheoryData<string>();
        foreach (AppearanceFamily family in Families.Value) data.Add(family.Id);
        return data;
    }

    private static IEnumerable<(int Frontage, int Depth, int Storeys, AttachedSides Sides, bool Corner, FamilyBodyMesh Mesh)> Bodies(string id)
    {
        FamilyBody body = Family(id).Body!;
        int pitch = body.Shop!.PitchDegrees is { } range ? (int)range.High : 0;
        foreach (int frontage in (int[])[4, 8, 12, 24])
        {
            foreach (int depth in (int[])[8, 24])
            {
                foreach (int storeys in (int[])[2, 4])
                {
                    foreach (AttachedSides sides in Sides)
                    {
                        foreach (bool corner in (bool[])[false, true])
                        {
                            int seed = (frontage * 7) + depth + storeys;
                            yield return (frontage, depth, storeys, sides, corner,
                                FamilyBodyBuilder.BuildShop(body, frontage, depth, storeys, sides, corner, seed, seed * 3, new TerracePlace(pitch, 0, 2)));
                        }
                    }
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(Forms))]
    public void Every_triangle_faces_the_way_its_normal_says(string family)
    {
        var wrong = new List<string>();
        foreach ((int frontage, int depth, int storeys, AttachedSides sides, bool corner, FamilyBodyMesh mesh) in Bodies(family))
        {
            foreach ((string name, ShellMesh part) in mesh.Parts)
            {
                ReadOnlySpan<Vector3> p = part.Positions;
                ReadOnlySpan<int> i = part.Indices;
                for (int t = 0; t < i.Length; t += 3)
                {
                    Vector3 winding = Vector3.Cross(p[i[t + 2]] - p[i[t]], p[i[t + 1]] - p[i[t]]);
                    if (Vector3.Dot(winding, part.Normals[i[t]]) <= 0f) wrong.Add($"{frontage}x{depth} {storeys} {sides} corner {corner}: {name} at {p[i[t]]} {p[i[t + 1]]} {p[i[t + 2]]}");
                }
            }
        }

        Assert.True(wrong.Count == 0, string.Join('\n', wrong.Take(10)));
    }

    /// <remarks>
    /// Awnings, signs and the 1960s canopy reach about 2 m over the pavement, and a free side's verge,
    /// canopy or turret about as far over the side street. A turret's spire tops the eaves by 8.2 m.
    /// </remarks>
    [Theory]
    [MemberData(nameof(Forms))]
    public void A_body_stays_on_its_Lot_and_stops_at_its_party_walls(string family)
    {
        var outside = new List<string>();
        foreach ((int frontage, int depth, int storeys, AttachedSides sides, bool corner, FamilyBodyMesh mesh) in Bodies(family))
        {
            float x = frontage / 2f, y = depth / 2f;
            float left = sides.HasFlag(AttachedSides.Left) ? x : x + 2.2f;
            float right = sides.HasFlag(AttachedSides.Right) ? x : x + 2.2f;
            foreach ((string part, ShellMesh shell) in mesh.Parts)
            {
                foreach (Vector3 p in shell.Positions)
                {
                    if (p.X < -left - 1e-3f || p.X > right + 1e-3f || p.Z < -y - 1.1f || p.Z > y + 2.1f
                        || p.Y < -1e-3f || p.Y > (storeys * ShellBuilder.StoreyMetres) + 8.3f)
                    {
                        outside.Add($"{family} {frontage}x{depth} {storeys} {sides} corner {corner}: {part} at {p}");
                        break;
                    }
                }
            }
        }

        Assert.True(outside.Count == 0, string.Join('\n', outside.DistinctBy(o => o.Split(':')[1].Split(' ')[1] + o.Split(' ')[0]).Take(40)));
    }

    [Theory]
    [MemberData(nameof(Forms))]
    public void A_pitched_roof_faces_the_sky(string family)
    {
        if (Family(family).Body!.Shop!.PitchDegrees is null) return;
        foreach ((_, _, _, _, _, FamilyBodyMesh mesh) in Bodies(family))
        {
            foreach ((string part, ShellMesh roof) in mesh.Parts)
            {
                if (!part.StartsWith("roof", StringComparison.Ordinal)) continue;
                foreach (Vector3 normal in roof.Normals) Assert.True(normal.Y > .2f);
            }
        }
    }

    [Fact]
    public void A_parade_shares_its_walls_and_each_shop_draws_its_own_front()
    {
        FamilyBody body = Family("shop-house-victorian-gable").Body!;
        var walls = new HashSet<string>(StringComparer.Ordinal);
        var fronts = new HashSet<string>(StringComparer.Ordinal);
        for (int seed = 0; seed < 16; seed++)
        {
            FamilyBodyMesh mesh = FamilyBodyBuilder.BuildShop(body, 8f, 14f, 3, AttachedSides.Left | AttachedSides.Right, false, seed, 5, new TerracePlace(38, 0, 3));
            walls.Add(string.Join(' ', mesh.Parts.Where(p => p.Part.StartsWith("wall", StringComparison.Ordinal)).Select(p => p.Part + Paints(p.Mesh))));
            fronts.Add(string.Join(' ', mesh.Parts.Where(p => p.Part is "door" or "frame" or "trim").Select(p => p.Part + Paints(p.Mesh))));
        }

        Assert.True(walls.Count == 1, string.Join(' ', walls));
        Assert.True(fronts.Count >= 8, string.Join('\n', fronts));
    }

    private static string Paints(ShellMesh mesh) =>
        string.Join(',', mesh.Colors.ToArray().Distinct().Order(Comparer<Vector4>.Create((a, b) => (a.X, a.Y, a.Z).CompareTo((b.X, b.Y, b.Z)))));

    [Theory]
    [InlineData("shop-house-turret-corner")]
    [InlineData("shop-house-interwar-curve")]
    [InlineData("shop-house-main-street")]
    [InlineData("shop-house-victorian-gable")]
    public void A_free_corner_cuts_back_its_ground_storey(string id)
    {
        FamilyBody body = Family(id).Body!;
        int pitch = body.Shop!.PitchDegrees is { } range ? (int)range.Low : 0;
        FamilyBodyMesh square = FamilyBodyBuilder.BuildShop(body, 12f, 14f, 3, AttachedSides.Left, false, 1, 1, new TerracePlace(pitch, 1, 2));
        FamilyBodyMesh cut = FamilyBodyBuilder.BuildShop(body, 12f, 14f, 3, AttachedSides.Left, true, 1, 1, new TerracePlace(pitch, 1, 2));

        Assert.True(NearCorner(square));
        Assert.False(NearCorner(cut));
    }

    /// <summary>Whether a wall stands within 30 cm of the street corner on the right, below the fascia.</summary>
    private static bool NearCorner(FamilyBodyMesh mesh) => mesh.Parts
        .Where(p => p.Part.StartsWith("wall", StringComparison.Ordinal))
        .SelectMany(p => p.Mesh.Positions.ToArray())
        .Any(p => p.Y < 2.5f && MathF.Abs(p.X - 6f) < .3f && MathF.Abs(p.Z - 7f) < .3f);

    [Fact]
    public void A_one_unit_frontage_holds_one_shop_and_a_wide_one_holds_several()
    {
        Assert.Single(FamilyBodyBuilder.ShopUnits(4f));
        Assert.Equal(4, FamilyBodyBuilder.ShopUnits(24f).Length);
        Assert.Equal(24f, FamilyBodyBuilder.ShopUnits(24f)[^1].To);
    }
}
