using Borough.Appearance;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Space;

namespace Borough.Tests.Appearance;

public class StylePresetTests
{
    private const string Header = """
        [preset]
        name = "fixture"
        era_days = 30
        attached = ["perimeter"]

        """;

    private static readonly WorldKey World = WorldKey.FromSeed(7);

    private static StylePresetResult Read(string text) => StylePresetReader.Read([("fixture.toml", Header + text)]);

    private static StylePreset Preset(string text)
    {
        StylePresetResult result = Read(text);
        Assert.True(result.Errors.Count == 0, string.Join('\n', result.Errors));
        return result.Preset!;
    }

    private static BuildingFacts Facts(ulong id, int storeys = 2, int frontage = 36, int depth = 20,
        BlockPattern pattern = BlockPattern.Detached, ulong face = 0, long raisedDay = 0, string kind = "dwelling",
        int offset = 0, int parcel = 0) =>
        new(id, kind, frontage, depth, storeys, pattern, LotTable.Housing, raisedDay, face, StreetSide.Left,
            Offset: offset, ParcelFrontage: parcel);

    private static string EightFamilies => string.Concat(Enumerable.Range(0, 8).Select(i => $"""
        [[family]]
        id = "f{i}"
        kinds = ["dwelling"]
        [family.body]
        library = "l"
        shape = "house"
        variant = "hipped"
        [[family.paint]]
        wall = "aa0000"
        [[family.paint]]
        wall = "00aa00"
        [[family.paint]]
        wall = "0000aa"

        """));

    private static StylePreset RunPreset() => StylePresetReader.Read([("fixture.toml", """
        [preset]
        name = "fixture"
        era_days = 30
        attached = ["perimeter"]
        runs = { alone = 3, matched = 5, copies = 2 }
        run_houses = [2, 4]

        """ + EightFamilies)]).Preset!;

    [Fact]
    public void Detached_houses_on_one_face_stand_in_runs_that_share_by_weight()
    {
        StylePreset preset = RunPreset();
        var counts = new Dictionary<RunShare, int>();
        bool aloneVaries = false;
        for (ulong face = 1; face <= 300; face++)
        {
            BuildingFacts[] lots = [.. Enumerable.Range(0, 12).Select(i =>
                Facts((face * 100) + (ulong)i, face: face, offset: (4 * i) + 2, parcel: 4))];
            foreach (IGrouping<HouseRun, BuildingFacts> run in lots.GroupBy(f => FamilyPicker.RunOf(preset, World, f)))
            {
                BuildingFacts[] houses = [.. run];
                int first = Array.IndexOf(lots, houses[0]);
                Assert.Equal(lots[first..(first + houses.Length)], houses);
                Assert.InRange(houses.Length, 1, 4);
                if (first + houses.Length < lots.Length) Assert.InRange(houses.Length, 2, 4);

                RunShare share = run.Key.Share;
                counts[share] = counts.GetValueOrDefault(share) + 1;
                string[] families = [.. houses.Select(f => FamilyPicker.Pick(preset, World, f).Family!.Id)];
                (int House, int Pitch)[] draws = [.. houses.Select(f => FamilyPicker.House(preset, World, f))];
                int[] paints = [.. houses.Select(f => FamilyPicker.Paint(FamilyPicker.Pick(preset, World, f).Family!, preset, World, f))];
                switch (share)
                {
                    case RunShare.Copies:
                        Assert.Single(families.Distinct());
                        Assert.Single(draws.Distinct());
                        Assert.Single(paints.Distinct());
                        break;
                    case RunShare.Matched:
                        Assert.Single(families.Distinct());
                        Assert.Single(draws.Select(d => d.Pitch).Distinct());
                        break;
                    default:
                        Assert.All(draws, d => Assert.Equal(d.House, d.Pitch));
                        aloneVaries |= families.Distinct().Count() > 1;
                        break;
                }
            }
        }

        int runs = counts.Values.Sum();
        Assert.InRange(counts[RunShare.Alone] * 10, runs * 2, runs * 4);
        Assert.InRange(counts[RunShare.Matched] * 10, runs * 4, runs * 6);
        Assert.InRange(counts[RunShare.Copies] * 10, runs * 1, runs * 3);
        Assert.True(aloneVaries);
    }

    [Fact]
    public void Runs_leave_attached_Buildings_and_presets_without_runs_alone()
    {
        StylePreset runs = RunPreset();
        StylePreset none = Preset(EightFamilies);

        Assert.Equal(new HouseRun(9, default, RunShare.Alone), FamilyPicker.RunOf(none, World, Facts(9, face: 5, offset: 2, parcel: 4)));
        Assert.Equal(RunShare.Alone, FamilyPicker.RunOf(runs, World, Facts(9, pattern: BlockPattern.Perimeter, face: 5, offset: 2, parcel: 4)).Share);
        Assert.Equal(RunShare.Alone, FamilyPicker.RunOf(runs, World, Facts(9, offset: 2, parcel: 4)).Share);
    }

    private static string TerraceFamilies(params (string Form, string Pitch, int High)[] forms) => string.Concat(forms.Select((f, i) => $"""
        [[family]]
        id = "t{i}"
        kinds = ["dwelling"]
        frontage_metres = [4, {f.High}]
        patterns = ["perimeter"]
        [family.body]
        library = "l"
        bay_metres = 3.5
        terrace = "{f.Form}"
        {f.Pitch}

        """));

    private static StylePreset TerracePreset() => Preset("terrace_houses = [3, 6]\n" + TerraceFamilies(
        ("side-gable", "pitch_degrees = [20, 40]", 12), ("parapet", "", 12), ("mansard", "", 12),
        ("front-gable", "pitch_degrees = [30, 45]", 12), ("paired-gable", "pitch_degrees = [25, 35]", 11),
        ("bay-front", "pitch_degrees = [20, 40]", 12)));

    [Fact]
    public void Attached_houses_stand_in_terraces_that_share_a_form_and_pitch_and_differ_from_their_neighbors()
    {
        StylePreset preset = TerracePreset();
        var seen = new HashSet<string>();
        for (ulong face = 1; face <= 200; face++)
        {
            BuildingFacts[] lots = [.. Enumerable.Range(0, 14).Select(i =>
                Facts((face * 100) + (ulong)i, frontage: 8, depth: 12, pattern: BlockPattern.Perimeter, face: face, offset: (2 * i) + 1, parcel: 2))];
            (string Family, int Pitch, Terrace Terrace)[] houses = [.. lots.Select(f =>
            {
                AppearanceFamily family = FamilyPicker.Pick(preset, World, f).Family!;
                Terrace terrace = FamilyPicker.TerraceOf(preset, World, f)!.Value;
                return (family.Id, FamilyPicker.TerracePitch(family, World, terrace), terrace);
            })];

            for (int i = 0; i < houses.Length; i++)
            {
                Assert.Equal(i % houses[i].Terrace.Houses, houses[i].Terrace.Index);
                Assert.InRange(houses[i].Terrace.Houses, 3, 6);
                seen.Add(houses[i].Family);
                if (i == 0) continue;
                bool inside = houses[i].Terrace.Cell == houses[i - 1].Terrace.Cell;
                Assert.Equal(inside, houses[i].Family == houses[i - 1].Family);
                if (inside) Assert.Equal(houses[i - 1].Pitch, houses[i].Pitch);
            }
        }

        Assert.Equal(6, seen.Count);
    }

    [Fact]
    public void A_terrace_keeps_its_form_where_a_neighbor_admits_one_family_fewer()
    {
        StylePreset preset = TerracePreset();
        int pairs = 0, alike = 0;
        for (ulong face = 1; face <= 200; face++)
        {
            BuildingFacts[] lots = [.. Enumerable.Range(0, 14).Select(i =>
                Facts((face * 100) + (ulong)i, frontage: i % 2 == 0 ? 8 : 12, depth: 12, pattern: BlockPattern.Perimeter, face: face, offset: (3 * i) + 1, parcel: 3))];
            for (int i = 1; i < lots.Length; i++)
            {
                if (FamilyPicker.TerraceOf(preset, World, lots[i])!.Value.Cell == FamilyPicker.TerraceOf(preset, World, lots[i - 1])!.Value.Cell) continue;
                pairs++;
                if (FamilyPicker.Pick(preset, World, lots[i]).Family == FamilyPicker.Pick(preset, World, lots[i - 1]).Family) alike++;
            }
        }

        Assert.InRange(alike * 20, 0, pairs);
    }

    private static readonly (string Form, string Pitch)[] ApartmentForms =
        [("parapet", ""), ("hipped", "pitch_degrees = [22, 32]"), ("mansard", ""), ("forecourt", "")];

    private static string ApartmentFamilies(params (string Group, int Weight)[] groups) => string.Concat(groups.SelectMany(g =>
        ApartmentForms.Select(f => $"""
        [[family]]
        id = "{g.Group}-{f.Form}"
        kinds = ["dwelling"]
        weight = {g.Weight}
        frontage_metres = [20, 28]
        [family.body]
        library = "l"
        bay_metres = 3.0
        apartment = "{f.Form}"
        {f.Pitch}

        """)));

    private static StylePreset ApartmentPreset(string more = "") =>
        Preset("apartment_lots = [1, 3]\n" + ApartmentFamilies(("corridor", 2), ("two-unit", 1)) + more);

    [Theory]
    [InlineData(BlockPattern.Perimeter)]
    [InlineData(BlockPattern.Detached)]
    public void Apartments_stand_in_runs_that_share_a_form_and_pitch_and_differ_from_their_neighbors(BlockPattern pattern)
    {
        StylePreset preset = ApartmentPreset();
        var forms = new HashSet<ApartmentForm>();
        var families = new HashSet<string>();
        for (ulong face = 1; face <= 200; face++)
        {
            (AppearanceFamily Family, int Pitch, Terrace Run)[] blocks = [.. Enumerable.Range(0, 14).Select(i =>
            {
                BuildingFacts facts = Facts((face * 100) + (ulong)i, storeys: 3, frontage: 24, depth: 16, pattern: pattern, face: face,
                    raisedDay: 40 * i, offset: (6 * i) + 1, parcel: 6);
                AppearanceFamily family = FamilyPicker.Pick(preset, World, facts).Family!;
                Terrace run = FamilyPicker.ApartmentRunOf(preset, World, facts)!.Value;
                return (family, FamilyPicker.TerracePitch(family, World, run), run);
            })];

            for (int i = 0; i < blocks.Length; i++)
            {
                Assert.Equal(i % blocks[i].Run.Houses, blocks[i].Run.Index);
                Assert.InRange(blocks[i].Run.Houses, 1, 3);
                forms.Add(blocks[i].Family.Body!.Apartment!.Form);
                families.Add(blocks[i].Family.Id);
                if (i == 0) continue;
                bool inside = blocks[i].Run.Cell == blocks[i - 1].Run.Cell;
                Assert.Equal(inside, blocks[i].Family.Body!.Apartment!.Form == blocks[i - 1].Family.Body!.Apartment!.Form);
                if (inside) Assert.Equal(blocks[i - 1].Pitch, blocks[i].Pitch);
            }
        }

        Assert.Equal(4, forms.Count);
        Assert.Equal(8, families.Count);
    }

    /// <remarks>The apartment families weigh 12 over 4 forms, so they draw as one family of weight 3.</remarks>
    [Fact]
    public void Apartment_forms_take_no_more_Lots_than_one_apartment_family_would()
    {
        StylePreset preset = ApartmentPreset("""
            [[family]]
            id = "house"
            kinds = ["dwelling"]
            weight = 3
            frontage_metres = [20, 28]
            [family.body]
            library = "l"
            bay_metres = 3.0

            """);
        int houses = 0;
        for (ulong face = 1; face <= 2000; face++)
        {
            BuildingFacts facts = Facts(face, storeys: 3, frontage: 24, depth: 16, face: face, offset: 1, parcel: 6);
            if (FamilyPicker.Pick(preset, World, facts).Family!.Id == "house") houses++;
        }

        Assert.InRange(houses, 850, 1150);
    }

    [Theory]
    [InlineData("apartment_lots = [0, 2]\n", "'apartment_lots' must be")]
    [InlineData("[[family]]\nid = \"a\"\nkinds = [\"dwelling\"]\n[family.body]\nlibrary = \"l\"\nbay_metres = 3\napartment = \"tower\"\n", "'apartment' must be")]
    [InlineData("[[family]]\nid = \"a\"\nkinds = [\"dwelling\"]\n[family.body]\nlibrary = \"l\"\nbay_metres = 3\napartment = \"hipped\"\n", "needs 'pitch_degrees'")]
    [InlineData("[[family]]\nid = \"a\"\nkinds = [\"dwelling\"]\n[family.body]\nlibrary = \"l\"\nbay_metres = 3\napartment = \"setback\"\npitch_degrees = [20, 30]\n", "takes no 'pitch_degrees'")]
    [InlineData("[[family]]\nid = \"a\"\nkinds = [\"dwelling\"]\n[family.body]\nlibrary = \"l\"\nbay_metres = 3\napartment = \"side-gable\"\npitch_degrees = [20, 30]\nparapet_metres = 0.7\n", "takes no 'parapet_metres'")]
    [InlineData("[[family]]\nid = \"a\"\nkinds = [\"dwelling\"]\n[family.body]\nlibrary = \"l\"\nbay_metres = 3\napartment = \"parapet\"\nterrace = \"parapet\"\n", "'terrace' or 'apartment', not both")]
    public void A_bad_apartment_is_refused(string text, string message)
    {
        Assert.Contains(Read(text).Errors, e => e.Message.Contains(message, StringComparison.Ordinal));
    }

    private static readonly (string Form, string Pitch)[] ShopForms =
        [("victorian-gable", "pitch_degrees = [34, 42]"), ("main-street", ""), ("sixties-canopy", ""), ("pantile-hipped", "pitch_degrees = [20, 28]")];

    private static StylePreset ShopPreset() => Preset("shop_lots = [2, 4]\n" + string.Concat(ShopForms.Select(f => $"""
        [[family]]
        id = "shop-{f.Form}"
        kinds = ["shopfront"]
        [family.body]
        library = "l"
        shape = "shop"
        variant = "{f.Form}"
        {f.Pitch}

        """)));

    [Fact]
    public void Shops_stand_in_parades_that_share_a_form_and_pitch_and_differ_from_their_neighbors()
    {
        StylePreset preset = ShopPreset();
        var forms = new HashSet<ShopForm>();
        for (ulong face = 1; face <= 200; face++)
        {
            (AppearanceFamily Family, int Pitch, Terrace Run)[] shops = [.. Enumerable.Range(0, 14).Select(i =>
            {
                BuildingFacts facts = Facts((face * 100) + (ulong)i, storeys: 3, frontage: 8, depth: 14, pattern: BlockPattern.ShopHouseParade,
                    face: face, raisedDay: 40 * i, kind: "shopfront", offset: (2 * i) + 1, parcel: 2);
                AppearanceFamily family = FamilyPicker.Pick(preset, World, facts).Family!;
                Terrace run = FamilyPicker.ShopRunOf(preset, World, facts)!.Value;
                return (family, FamilyPicker.TerracePitch(family, World, run), run);
            })];

            for (int i = 0; i < shops.Length; i++)
            {
                Assert.InRange(shops[i].Run.Houses, 2, 4);
                forms.Add(shops[i].Family.Body!.Shop!.Form);
                if (i == 0) continue;
                bool inside = shops[i].Run.Cell == shops[i - 1].Run.Cell;
                Assert.Equal(inside, shops[i].Family.Body!.Shop!.Form == shops[i - 1].Family.Body!.Shop!.Form);
                if (inside) Assert.Equal(shops[i - 1].Pitch, shops[i].Pitch);
            }
        }

        Assert.Equal(4, forms.Count);
    }

    [Theory]
    [InlineData("shop_lots = [0, 2]\n", "'shop_lots' must be")]
    [InlineData("[[family]]\nid = \"a\"\nkinds = [\"shopfront\"]\n[family.body]\nlibrary = \"l\"\nshape = \"shop\"\nvariant = \"arcade\"\n", "a shop 'variant' must be")]
    [InlineData("[[family]]\nid = \"a\"\nkinds = [\"shopfront\"]\n[family.body]\nlibrary = \"l\"\nshape = \"shop\"\nvariant = \"turret-corner\"\n", "needs 'pitch_degrees'")]
    [InlineData("[[family]]\nid = \"a\"\nkinds = [\"shopfront\"]\n[family.body]\nlibrary = \"l\"\nshape = \"shop\"\nvariant = \"mansard\"\npitch_degrees = [20, 30]\n", "takes no 'pitch_degrees'")]
    [InlineData("[[family]]\nid = \"a\"\nkinds = [\"shopfront\"]\n[family.body]\nlibrary = \"l\"\nshape = \"shop\"\nvariant = \"main-street\"\nbay_metres = 3\n", "only valid for a block body")]
    public void A_bad_shop_is_refused(string text, string message)
    {
        Assert.Contains(Read(text).Errors, e => e.Message.Contains(message, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("terrace_houses = [0, 4]\n", "with low at least 1")]
    [InlineData("[[family]]\nid = \"a\"\nkinds = [\"dwelling\"]\n[family.body]\nlibrary = \"l\"\nbay_metres = 3.5\nterrace = \"gambrel\"\n", "'terrace' must be")]
    [InlineData("[[family]]\nid = \"a\"\nkinds = [\"dwelling\"]\n[family.body]\nlibrary = \"l\"\nbay_metres = 3.5\nterrace = \"side-gable\"\n", "needs 'pitch_degrees'")]
    [InlineData("[[family]]\nid = \"a\"\nkinds = [\"dwelling\"]\n[family.body]\nlibrary = \"l\"\nbay_metres = 3.5\nterrace = \"side-gable\"\npitch_degrees = [10, 30]\n", "within 18 to 45")]
    [InlineData("[[family]]\nid = \"a\"\nkinds = [\"dwelling\"]\n[family.body]\nlibrary = \"l\"\nbay_metres = 3.5\nterrace = \"parapet\"\npitch_degrees = [20, 30]\n", "takes no 'pitch_degrees'")]
    [InlineData("[[family]]\nid = \"a\"\nkinds = [\"dwelling\"]\n[family.body]\nlibrary = \"l\"\nbay_metres = 3.5\nterrace = \"mansard\"\ngable_degrees = 30\n", "takes no 'gable_degrees'")]
    [InlineData("[[family]]\nid = \"a\"\nkinds = [\"dwelling\"]\n[family.body]\nlibrary = \"l\"\nbay_metres = 3.5\npitch_degrees = [20, 30]\n", "'pitch_degrees' needs 'terrace'")]
    public void A_bad_terrace_is_refused(string text, string message)
    {
        Assert.Contains(Read(text).Errors, e => e.Message.Contains(message, StringComparison.Ordinal));
    }

    [Fact]
    public void Terraces_need_attached_patterns()
    {
        StylePresetResult result = StylePresetReader.Read([("fixture.toml", "[preset]\nname = \"x\"\nera_days = 1\nterrace_houses = [3, 6]\n")]);

        Assert.Contains(result.Errors, e => e.Message.Contains("needs 'attached'", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("runs = { alone = 1 }\n", "'runs' needs 'run_houses'")]
    [InlineData("run_houses = [2, 4]\n", "'run_houses' needs 'runs'")]
    [InlineData("runs = { alone = 0 }\nrun_houses = [2, 4]\n", "at least one run a weight above 0")]
    [InlineData("runs = { lonely = 1 }\nrun_houses = [2, 4]\n", "'runs.lonely' is not a run")]
    [InlineData("runs = { alone = 1 }\nrun_houses = [0, 4]\n", "with low at least 1")]
    public void A_bad_run_table_is_refused(string keys, string message)
    {
        StylePresetResult result = StylePresetReader.Read([("fixture.toml", "[preset]\nname = \"x\"\nera_days = 1\n" + keys)]);

        Assert.Contains(result.Errors, e => e.Message.Contains(message, StringComparison.Ordinal));
    }

    [Fact]
    public void The_shipped_test_street_preset_reads_clean()
    {
        StylePresetResult result = StylePresetReader.Read(Path.Combine(AppContext.BaseDirectory, "Appearance", "test-street"));

        Assert.Empty(result.Errors);
        Assert.Equal("test-street", result.Preset!.Name);
        Assert.Contains(result.Preset.Families, f => f.Fallback && f.Kinds.Contains("dwelling"));
    }

    [Fact]
    public void The_shipped_test_street_preset_dresses_only_from_textures_the_library_holds()
    {
        StylePreset preset = StylePresetReader.Read(Path.Combine(AppContext.BaseDirectory, "Appearance", "test-street")).Preset!;
        var library = TextureLibrary.Read(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Appearance", "library", "materials.json")));

        StylePresetResult dressed = library.Dress(preset);

        Assert.Empty(dressed.Errors);
        Assert.Contains(dressed.Preset!.Families, f => f.Body?.Materials.Count > 0);
    }

    [Theory]
    [InlineData("[[family]]\nid = \"a\"\nkinds = [\"dwelling\"]\ncolour = \"red\"\n", 8, "unknown key 'colour'")]
    [InlineData("[[family]]\nid = \"a\"\nkinds = [\"dwelling\"]\nstoreys = [3, 2]\n", 8, "'storeys' must be [low, high]")]
    [InlineData("[[family]]\nid = \"a\"\nkinds = [\"dwelling\"]\npatterns = [\"terrace\"]\n", 8, "'terrace' is not a patterns value")]
    [InlineData("[[family]]\nid = \"a\"\nkinds = [\"dwelling\"]\nparking = [\"garage\"]\n", 8, "'garage' is not a parking value")]
    [InlineData("[[family]]\nid = \"a\"\nkinds = [\"dwelling\"]\nanchored = 1\n", 8, "'anchored' must be true or false")]
    [InlineData("[[family]]\nid = \"a\"\nkinds = [\"dwelling\"]\nweight = 0\n", 8, "'weight' must be a whole number of at least 1")]
    [InlineData("[[family]]\nid = \"a\"\nfallback = true\nkinds = [\"dwelling\"]\nstoreys = [1, 2]\n", 9, "fallback family admits every Building")]
    [InlineData("[[family]]\nkinds = [\"dwelling\"]\n", 5, "'id' is required")]
    [InlineData("[[building]]\nid = \"a\"\n", 5, "unknown section 'building'")]
    [InlineData("[[family]]\nid = \"a\"\nkinds = [\"dwelling\"]\n[family.body]\nlibrary = \"l\"\nbay_metres = 6\n[[family.paint]]\nwall = \"red\"\n", 12, "'wall' must be an sRGB colour")]
    [InlineData("[[family]]\nid = \"a\"\nkinds = [\"dwelling\"]\n[family.body]\nlibrary = \"l\"\nbay_metres = 6\n[[family.paint]]\nglazing = \"ffffff\"\n", 12, "unknown key 'glazing'")]
    [InlineData("[[family]]\nid = \"a\"\nkinds = [\"dwelling\"]\n[family.body]\nlibrary = \"l\"\nbay_metres = 6\n[[family.paint]]\nweight = 2\n", 11, "must colour at least one part")]
    [InlineData("[[family]]\nid = \"a\"\nkinds = [\"dwelling\"]\n[[family.paint]]\nwall = \"ffffff\"\n", 5, "has paint but no [family.body]")]
    [InlineData("[[family]]\nid = \"a\"\nkinds = [\"dwelling\"]\n[family.body]\nlibrary = \"l\"\nbay_metres = 6\nmaterials = { chimney = \"bricks-088\" }\n", 11, "'materials.chimney' names no body part")]
    [InlineData("[[family]]\nid = \"a\"\nkinds = [\"dwelling\"]\n[family.body]\nlibrary = \"l\"\nbay_metres = 6\nmaterials = { wall = 3 }\n", 11, "'materials.wall' must name a texture library entry")]
    public void A_mistake_is_refused_at_its_line(string text, int line, string message)
    {
        StylePresetResult result = Read(text);

        Assert.Null(result.Preset);
        AppearanceDiagnostic error = Assert.Single(result.Errors);
        Assert.Equal(line, error.Line);
        Assert.Contains(message, error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Two_fallbacks_for_one_kind_and_a_repeated_id_are_refused()
    {
        StylePresetResult result = Read("""
            [[family]]
            id = "a"
            fallback = true
            kinds = ["dwelling"]

            [[family]]
            id = "a"
            fallback = true
            kinds = ["dwelling"]
            """);

        Assert.Null(result.Preset);
        Assert.Contains(result.Errors, e => e.Message.Contains("family id 'a' is already declared", StringComparison.Ordinal));
        Assert.Contains(result.Errors, e => e.Message.Contains("kind 'dwelling' already has fallback", StringComparison.Ordinal));
    }

    [Fact]
    public void A_ruleset_check_names_kinds_without_a_fallback_and_kinds_it_does_not_declare()
    {
        StylePreset preset = Preset("""
            [[family]]
            id = "box"
            fallback = true
            kinds = ["dwelling", "lighthouse"]
            """);

        IReadOnlyList<AppearanceDiagnostic> warnings = StylePresetReader.CheckAgainst(preset, ["dwelling", "school"]);

        Assert.Equal(2, warnings.Count);
        Assert.Contains(warnings, w => w.Message == "kind 'school' has no fallback family.");
        Assert.Contains(warnings, w => w.Message.EndsWith("does not declare: 'lighthouse'.", StringComparison.Ordinal));
    }

    [Fact]
    public void The_fallback_draws_only_what_no_family_admits_and_a_kind_without_one_is_missing()
    {
        StylePreset preset = Preset("""
            [[family]]
            id = "box"
            fallback = true
            kinds = ["dwelling"]

            [[family]]
            id = "workplace"
            kinds = ["dwelling"]
            storeys = [2, 2]
            frontage_metres = [36, 36]
            """);

        Assert.Equal("workplace", FamilyPicker.Pick(preset, World, Facts(1)).Family!.Id);
        Assert.Equal(new FamilyPick(preset.Families[0], FamilyChoice.Fallback), FamilyPicker.Pick(preset, World, Facts(2, storeys: 3)));
        Assert.Equal(FamilyChoice.Missing, FamilyPicker.Pick(preset, World, Facts(3, kind: "school")).Choice);
    }

    [Fact]
    public void A_centre_family_admits_centres_by_form_units_anchor_and_parking()
    {
        StylePreset preset = Preset("""
            [[family]]
            id = "strip"
            kinds = ["shopfront"]
            patterns = ["car-park-centre"]
            units = [4, 12]
            anchored = true
            parking = ["surface"]
            """);

        AppearanceFamily strip = preset.Families[0];
        BuildingFacts centre = Facts(1, storeys: 1, pattern: BlockPattern.CarParkCentre, kind: "shopfront") with
        {
            Units = 8,
            Anchored = true,
            Parking = ParkingForm.Surface,
        };

        Assert.True(strip.Admits(centre));
        Assert.False(strip.Admits(centre with { Units = 13 }));
        Assert.False(strip.Admits(centre with { Anchored = false }));
        Assert.False(strip.Admits(centre with { Parking = ParkingForm.Deck }));
        Assert.False(strip.Admits(centre with { Pattern = BlockPattern.Perimeter }));
    }

    [Fact]
    public void A_corner_family_admits_only_corner_buildings()
    {
        StylePreset preset = Preset("""
            [[family]]
            id = "domed"
            kinds = ["shopfront"]
            corner = true
            """);

        AppearanceFamily domed = preset.Families[0];
        BuildingFacts shop = Facts(1, storeys: 3, pattern: BlockPattern.Perimeter, kind: "shopfront");

        Assert.True(domed.Admits(shop with { Corner = true }));
        Assert.False(domed.Admits(shop with { Corner = false }));
    }

    [Fact]
    public void Weights_share_the_draw_and_a_Building_always_draws_the_same_family()
    {
        StylePreset preset = Preset("""
            [[family]]
            id = "light"
            kinds = ["dwelling"]

            [[family]]
            id = "heavy"
            kinds = ["dwelling"]
            weight = 3
            """);

        int heavy = Enumerable.Range(1, 4000).Count(id => FamilyPicker.Pick(preset, World, Facts((ulong)id)).Family!.Id == "heavy");

        Assert.InRange(heavy, 2850, 3150);
        Assert.Equal(FamilyPicker.Pick(preset, World, Facts(17)), FamilyPicker.Pick(preset, World, Facts(17)));
    }

    [Fact]
    public void Attached_Buildings_on_one_face_in_one_era_share_a_family_and_later_infill_draws_afresh()
    {
        StylePreset preset = Preset(string.Concat(Enumerable.Range(0, 8).Select(i => $"""
            [[family]]
            id = "f{i}"
            kinds = ["dwelling"]

            """)));

        string[] Row(long day) => [.. Enumerable.Range(1, 12).Select(id =>
            FamilyPicker.Pick(preset, World, Facts((ulong)id, pattern: BlockPattern.Perimeter, face: 5, raisedDay: day)).Family!.Id)];

        Assert.Single(Row(0).Distinct());
        Assert.Equal(Row(0), Row(29));
        Assert.Contains(Enumerable.Range(1, 20), era => Row(era * 30)[0] != Row(0)[0]);

        string[] detached = [.. Enumerable.Range(1, 12).Select(id =>
            FamilyPicker.Pick(preset, World, Facts((ulong)id, face: 5)).Family!.Id)];
        Assert.True(detached.Distinct().Count() > 1);
    }

    [Fact]
    public void A_dressed_part_tiles_at_its_texture_size_unless_the_body_lists_it_and_an_unknown_texture_is_refused()
    {
        var library = TextureLibrary.Read("""
            { "textures": {
                "bricks-088": { "tile_metres": [1.5, 1.5], "paint": true, "albedo_linear_mean_luminance": 0.64 },
                "clay-roof-tiles-02": { "tile_metres": [2, 2], "paint": false, "albedo_linear_mean_luminance": 0.13 } } }
            """);
        Assert.Equal(["bricks-088"], library.Paintable);
        Assert.Equal(0.64f, library.MeanLuminance["bricks-088"]);
        StylePreset preset = Preset("""
            [[family]]
            id = "terrace"
            kinds = ["dwelling"]

            [family.body]
            library = "l"
            bay_metres = 6
            tile_metres = { roof = [4, 4], trim = [1, 1] }
            materials = { wall = "bricks-088", roof = "clay-roof-tiles-02" }
            """);

        FamilyBody body = library.Dress(preset).Preset!.Families[0].Body!;

        Assert.Equal((1.5f, 1.5f), body.TileMetres["wall"]);
        Assert.Equal((4f, 4f), body.TileMetres["roof"]);
        Assert.Equal((1f, 1f), body.TileMetres["trim"]);
        Assert.Equal("clay-roof-tiles-02", body.Materials["roof"]);

        StylePresetResult refused = (library with { TileMetres = new Dictionary<string, (float, float)>() }).Dress(preset);
        Assert.Null(refused.Preset);
        Assert.Equal(2, refused.Errors.Count);
        Assert.Contains(refused.Errors, e => e.Line == 5 && e.Message.Contains("dresses 'wall' in 'bricks-088'", StringComparison.Ordinal));
    }

    [Fact]
    public void Each_Building_draws_its_own_paint_scheme_by_weight_even_in_an_attached_run()
    {
        AppearanceFamily family = Assert.Single(Preset("""
            [[family]]
            id = "house"
            kinds = ["dwelling"]

            [family.body]
            library = "l"
            bay_metres = 6

            [[family.paint]]
            wall = "b9ad97"
            door = "4f5253"

            [[family.paint]]
            wall = "7d8b87"

            [[family.paint]]
            wall = "5b6770"
            weight = 2
            """).Families);

        int[] draws = [.. Enumerable.Range(1, 4000).Select(id => FamilyPicker.Paint(family, World, (ulong)id))];

        Assert.Equal(new PaintScheme(new Dictionary<string, Paint> { ["wall"] = new(0xb9, 0xad, 0x97), ["door"] = new(0x4f, 0x52, 0x53) }, 1).Parts,
            family.Paints![0].Parts);
        Assert.InRange(draws.Count(d => d == 2), 1850, 2150);
        Assert.Equal(FamilyPicker.Paint(family, World, 17), FamilyPicker.Paint(family, World, 17));
        Assert.True(draws.Take(12).Distinct().Count() > 1);
        Assert.Equal(-1, FamilyPicker.Paint(family with { Paints = null }, World, 17));
    }
}
