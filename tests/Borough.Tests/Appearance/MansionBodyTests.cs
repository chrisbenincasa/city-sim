using System.Numerics;
using System.Text.Json;
using Borough.Appearance;

namespace Borough.Tests.Appearance;

public sealed class MansionBodyTests
{
    public static TheoryData<string> Styles =>
        ["colonial", "tudor", "queen-anne", "chateau", "spanish", "prairie", "regency", "mid-century", "farmhouse", "neo-eclectic", "california"];

    private static readonly (float Frontage, float Depth, int Storeys)[] Sites = [(28f, 8f, 2), (36f, 12f, 3), (40f, 16f, 2), (48f, 20f, 3), (84f, 24f, 2)];

    [Theory]
    [MemberData(nameof(Styles))]
    public void The_same_seed_builds_the_same_house(string style)
    {
        FamilyBody body = House(style);

        Assert.Equal(Describe(FamilyBodyBuilder.BuildHouse(body, 44f, 18f, 2, 17)), Describe(FamilyBodyBuilder.BuildHouse(body, 44f, 18f, 2, 17)));
    }

    [Theory]
    [MemberData(nameof(Styles))]
    public void Different_seeds_build_different_houses_and_plans_on_one_lot(string style)
    {
        FamilyBody body = House(style);
        HouseVariant variant = body.House!.Variant;

        int houses = Enumerable.Range(0, 16).Select(seed => Describe(FamilyBodyBuilder.BuildHouse(body, 52f, 20f, 2, seed))).Distinct().Count();
        int plans = Enumerable.Range(0, 16).Select(seed => FamilyBodyBuilder.PlanMansion(variant, 52f, 20f, 2, seed, MansionGrounds.OneHousehold).Kind)
            .Distinct().Count();

        Assert.True(houses >= 14, $"{style}: 16 seeds built {houses} distinct houses");
        Assert.True(plans >= 2, $"{style}: 16 seeds drew {plans} plans");
    }

    /// <remarks>Eaves overhang the footprint by up to 1.8 m, and more at a turned wing's corners, and a chateau's chimneys stand over its steep roofs.</remarks>
    [Theory]
    [MemberData(nameof(Styles))]
    public void Every_draw_stays_on_its_lot_and_faces_the_way_it_is_wound(string style)
    {
        FamilyBody body = House(style);
        foreach ((float frontage, float depth, int storeys) in Sites)
        {
            foreach (MansionGrounds grounds in (MansionGrounds[])[MansionGrounds.OneHousehold, MansionGrounds.Apartments])
            {
                for (int seed = 0; seed < FamilyBodyBuilder.HouseSeeds; seed += 23)
                {
                    FamilyBodyMesh mesh = FamilyBodyBuilder.BuildHouse(body, frontage, depth, storeys, seed, grounds: grounds);
                    foreach ((string part, ShellMesh shell) in mesh.Parts)
                    {
                        string at = $"{style} {frontage}x{depth}x{storeys} {grounds} seed {seed} {part}";
                        ReadOnlySpan<Vector3> positions = shell.Positions;
                        foreach (Vector3 p in positions)
                        {
                            Assert.True(float.IsFinite(p.X) && float.IsFinite(p.Y) && float.IsFinite(p.Z), at);
                            Assert.True(MathF.Abs(p.X) <= (frontage / 2f) + .5f, $"{at}: x {p.X:F2}");
                            Assert.True(MathF.Abs(p.Z) <= (depth / 2f) + 2.5f, $"{at}: z {p.Z:F2}");
                            Assert.True(p.Y >= 0f && p.Y < (storeys * ShellBuilder.StoreyMetres) + 16f, $"{at}: y {p.Y:F2}");
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
    }

    /// <summary>The shell culls back faces, so a roof wound inward would leave a hole seen from above.</summary>
    [Theory]
    [MemberData(nameof(Styles))]
    public void Seen_from_above_every_house_shows_the_face_of_its_roofs(string style)
    {
        FamilyBody body = House(style);
        foreach ((float frontage, float depth, int storeys) in Sites)
        {
            for (int seed = 0; seed < FamilyBodyBuilder.HouseSeeds; seed += 41)
            {
                FamilyBodyMesh mesh = FamilyBodyBuilder.BuildHouse(body, frontage, depth, storeys, seed);
                for (int i = 0; i < 13; i++)
                {
                    for (int j = 0; j < 7; j++)
                    {
                        float x = (-frontage / 2f) + .37f + ((frontage - .74f) * i / 12f);
                        float z = (-depth / 2f) + .37f + ((depth - .74f) * j / 6f);
                        Assert.True(TopFacesUp(mesh, x, z), $"{style} {frontage}x{depth}x{storeys} seed {seed}: the top surface over ({x:F2}, {z:F2}) faces down");
                    }
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(Styles))]
    public void A_single_household_has_a_street_door_and_mostly_a_garage(string style)
    {
        FamilyBody body = House(style);
        int garaged = 0;
        for (int seed = 0; seed < 32; seed++)
        {
            FamilyBodyMesh mesh = FamilyBodyBuilder.BuildHouse(body, 48f, 22f, 2, seed, grounds: MansionGrounds.OneHousehold);
            MansionPlan plan = FamilyBodyBuilder.PlanMansion(body.House!.Variant, 48f, 22f, 2, seed, MansionGrounds.OneHousehold);

            Assert.NotEmpty(mesh.Doors);
            Assert.Equal(0, SideDoors(mesh));
            if (plan.Masses.Any(m => m.Part == MansionPart.Garage)) garaged++;
        }

        Assert.True(garaged >= 28, $"{style}: {garaged} of 32 single houses have a garage");
    }

    [Theory]
    [MemberData(nameof(Styles))]
    public void An_apartment_mansion_opens_side_doors_and_has_no_garage(string style)
    {
        FamilyBody body = House(style);
        for (int seed = 0; seed < 32; seed++)
        {
            FamilyBodyMesh mesh = FamilyBodyBuilder.BuildHouse(body, 48f, 22f, 2, seed, grounds: MansionGrounds.Apartments);
            MansionPlan plan = FamilyBodyBuilder.PlanMansion(body.House!.Variant, 48f, 22f, 2, seed, MansionGrounds.Apartments);

            Assert.DoesNotContain(plan.Masses, m => m.Part == MansionPart.Garage);
            Assert.True(SideDoors(mesh) >= 2, $"{style} seed {seed}: {SideDoors(mesh)} side doors");
        }
    }

    [Theory]
    [MemberData(nameof(Styles))]
    public void Every_texture_a_mansion_draws_is_a_house_texture(string style)
    {
        FamilyBody body = House(style);
        for (int seed = 0; seed < 64; seed++)
        {
            foreach (MansionGrounds grounds in (MansionGrounds[])[MansionGrounds.OneHousehold, MansionGrounds.Apartments])
            {
                foreach ((string part, _) in FamilyBodyBuilder.BuildHouse(body, 48f, 22f, 2, seed, grounds: grounds).Parts)
                {
                    int at = part.IndexOf('@', StringComparison.Ordinal);
                    if (at >= 0) Assert.Contains(part[(at + 1)..], FamilyBodyBuilder.HouseTextures);
                }
            }
        }
    }

    /// <summary>A deep yard leaves most mansions 8-13 m of footprint, so the wings must still fit there.</summary>
    [Theory]
    [MemberData(nameof(Styles))]
    public void A_shallow_house_behind_a_deep_yard_still_draws_wings(string style)
    {
        HouseVariant variant = House(style).House!.Variant;
        foreach ((float frontage, float depth) in ((float, float)[])[(40f, 11f), (48f, 12f)])
        {
            MansionPlanKind[] plans = [.. Enumerable.Range(0, 16).Select(seed => FamilyBodyBuilder.PlanMansion(variant, frontage, depth, 2, seed,
                MansionGrounds.Apartments).Kind)];
            int winged = plans.Count(p => p != MansionPlanKind.Bar);

            Assert.True(winged >= 8, $"{style} {frontage}x{depth}: {winged} of 16 draw wings ({string.Join(",", plans.Distinct())})");
        }
    }

    [Fact]
    public void A_house_too_narrow_for_its_wings_falls_back_to_one_block()
    {
        foreach (HouseVariant style in Enum.GetValues<HouseVariant>().Where(FamilyBodyBuilder.IsMansion))
        {
            MansionPlan plan = FamilyBodyBuilder.PlanMansion(style, 28f, 8f, 2, 3, MansionGrounds.OneHousehold);

            Assert.Equal(MansionPlanKind.Bar, plan.Kind);
        }
    }

    /// <summary><c>scripts/art/mansion-forms.py</c> runs the same grammar; its plans are the reference.</summary>
    [Fact]
    public void Plans_match_the_blender_reference()
    {
        using JsonDocument json = Bodies();
        int checkedPlans = 0;
        foreach (JsonElement entry in json.RootElement.GetProperty("plans").EnumerateArray())
        {
            HouseVariant style = House(entry.GetProperty("style").GetString()!).House!.Variant;
            MansionGrounds grounds = entry.GetProperty("grounds").GetString() == "single" ? MansionGrounds.OneHousehold : MansionGrounds.Apartments;
            MansionPlan plan = FamilyBodyBuilder.PlanMansion(style, entry.GetProperty("frontage").GetSingle(), entry.GetProperty("depth").GetSingle(),
                entry.GetProperty("storeys").GetInt32(), entry.GetProperty("seed").GetInt32(), grounds);
            string at = $"{entry.GetProperty("style")} {entry.GetProperty("frontage")}x{entry.GetProperty("depth")} seed {entry.GetProperty("seed")}";

            Assert.Equal(entry.GetProperty("plan").GetString(), Kebab(plan.Kind.ToString()));
            Assert.Equal(entry.GetProperty("garage_side").GetInt32(), plan.GarageSide);
            Assert.Equal(entry.GetProperty("entry").GetSingle(), plan.Entry, .01f);
            JsonElement[] masses = [.. entry.GetProperty("masses").EnumerateArray()];
            Assert.True(masses.Length == plan.Masses.Length, $"{at}: {plan.Masses.Length} masses against {masses.Length}");
            for (int i = 0; i < masses.Length; i++)
            {
                MansionMass m = plan.Masses[i];
                JsonElement e = masses[i];
                Assert.Equal(e.GetProperty("part").GetString(), Kebab(m.Part.ToString()));
                float[] box = [.. e.GetProperty("box").EnumerateArray().Select(v => v.GetSingle())];
                float[] mine = [m.X0, m.X1, m.Y0, m.Y1, m.Turn, m.PivotX, m.PivotY];
                for (int k = 0; k < box.Length; k++) Assert.True(MathF.Abs(box[k] - mine[k]) < .01f, $"{at} mass {i} value {k}: {mine[k]} against {box[k]}");
                Assert.Equal(e.GetProperty("storeys").GetInt32(), m.Storeys);
            }

            checkedPlans++;
        }

        Assert.True(checkedPlans >= 99, $"only {checkedPlans} reference plans");
    }

    [Fact]
    public void A_mansion_reads_its_yard_and_refuses_misplaced_yard_keys()
    {
        MansionYard yard = House("colonial").House!.Yard!.Value;

        Assert.Equal((16f, 20f), yard.Deep);
        Assert.Equal((8f, 10f), yard.Shallow);
        Assert.Equal(8f, yard.LeastHouse);
        Assert.True(House("colonial").House!.Wide);
        Assert.Contains(Read("shape = \"house\"\nvariant = \"villa\"\nyard_metres = [16, 20]").Errors, e => e.Message.Contains("yard_metres", StringComparison.Ordinal));
        Assert.Contains(Read("shape = \"house\"\nvariant = \"mansion-tudor\"").Errors, e => e.Message.Contains("yard_metres", StringComparison.Ordinal));
        Assert.Contains(Read("shape = \"house\"\nvariant = \"mansion-tudor\"\nwide = true\nyard_metres = [16, 20]\nshallow_yard_metres = [8, 10]\nleast_house_metres = 8").Errors,
            e => e.Message.Contains("always wide", StringComparison.Ordinal));
        Assert.Contains(Read("shape = \"house\"\nvariant = \"mansion-tudor\"\nyard_metres = [16, 20]\nshallow_yard_metres = [8, 18]\nleast_house_metres = 8").Errors,
            e => e.Message.Contains("shallow_yard_metres", StringComparison.Ordinal));
    }

    [Fact]
    public void A_mansion_has_no_shared_roof_pitch()
    {
        Assert.Equal(0, FamilyBodyBuilder.HousePitchDegrees(HouseVariant.MansionTudor, 5, 40f, 20f, 2));
    }

    /// <summary>Door leaves in walls that face neither the Street nor the back. A garage door stands on the ground, and a house door on its sill.</summary>
    private static int SideDoors(FamilyBodyMesh mesh)
    {
        int sides = 0;
        foreach ((string part, ShellMesh shell) in mesh.Parts)
        {
            if (part != "door") continue;
            ReadOnlySpan<Vector3> positions = shell.Positions;
            ReadOnlySpan<Vector3> normals = shell.Normals;
            for (int i = 0; i + 3 < positions.Length; i += 4)
            {
                float low = MathF.Min(MathF.Min(positions[i].Y, positions[i + 1].Y), MathF.Min(positions[i + 2].Y, positions[i + 3].Y));
                if (MathF.Abs(normals[i].Z) < .9f && low > .2f && low < .4f) sides++;
            }
        }

        return sides;
    }

    private static string Kebab(string name) => string.Concat(name.Select((c, i) => char.IsUpper(c) && i > 0 ? $"-{char.ToLowerInvariant(c)}" : $"{char.ToLowerInvariant(c)}"));

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

    private static JsonDocument Bodies()
    {
        DirectoryInfo? at = new(AppContext.BaseDirectory);
        while (at is not null && !File.Exists(Path.Combine(at.FullName, "CONTEXT.md"))) at = at.Parent;
        return JsonDocument.Parse(File.ReadAllText(Path.Combine(at!.FullName, "art", "mansion-forms", "bodies.json")));
    }

    private static FamilyBody House(string style)
    {
        string variant = style.StartsWith("mansion-", StringComparison.Ordinal) ? style : $"mansion-{style}";
        StylePresetResult read = Read($"shape = \"house\"\nvariant = \"{variant}\"\nyard_metres = [16, 20]\nshallow_yard_metres = [8, 10]\nleast_house_metres = 8");
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

    private static StylePresetResult Read(string body) => StylePresetReader.Read([("house.toml", $$"""
        [preset]
        name = "house"
        era_days = 1
        [[family]]
        id = "house"
        kinds = ["dwelling"]
        frontage_metres = [28, 84]
        [family.body]
        library = "library"
        {{body}}
        """)]);
}
