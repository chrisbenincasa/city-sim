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
        BlockPattern pattern = BlockPattern.Detached, ulong face = 0, long raisedDay = 0, string kind = "dwelling") =>
        new(id, kind, frontage, depth, storeys, pattern, LotTable.Housing, raisedDay, face, StreetSide.Left);

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
