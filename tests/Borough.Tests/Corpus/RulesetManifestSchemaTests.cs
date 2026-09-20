using System.Text.Json;
using Borough.Formats;
using Borough.Tests.Formats;

namespace Borough.Tests.Corpus;

/// <summary>
/// The committed manifest schema is what <c>RulesetCapture.ReadManifest</c> accepts, and this is the
/// line that keeps them together.
/// </summary>
/// <remarks>
/// <para>
/// <b>Unlike <c>rulesets/ruleset.schema.json</c> this one is hand-written.</b> The Ruleset schema is
/// generated because <c>RulesetLoader.Find</c> records every key a reader asks for, so the permitted
/// surface is derived and a second copy would be <c>plans/0012</c> <b>Cause 1</b>. No such mechanism
/// exists for a manifest — the loader never sees one — and the surface is two keys written into a
/// <c>switch</c>. ***A generator over a two-key list re-renders the list rather than deriving it***,
/// so the schema is authored and this drives every claim it makes through the resolver instead.
/// </para>
/// <para>
/// ⚠ <b>One direction is closed mechanically and the other is not.</b> A key in the schema that the
/// resolver refuses fails here, and a key the resolver requires that the schema omits fails here,
/// because both are checked against a manifest the resolver actually reads. What is not mechanical
/// is a <em>new optional</em> key added to <c>ReadManifest</c> and to nothing else: nothing lists
/// the reader's cases, so nothing can miss one. Source v1 has no optional keys, and adding one means
/// touching the schema in the same change.
/// </para>
/// </remarks>
public sealed class RulesetManifestSchemaTests
{
    private const string SchemaFile = "rulesets/manifest.schema.json";

    /// <summary>Every key the schema offers, and the resolver accepts a manifest stating them all.</summary>
    [Fact]
    public void The_schema_offers_exactly_what_a_working_manifest_states()
    {
        IReadOnlyList<string> offered = Offered();

        Assert.Equal(["members", "version"], offered.Order(StringComparer.Ordinal));

        RulesetSourceResult result = Resolve(SourcePackage.Manifest(Members));

        Assert.True(
            result.Ok,
            "the manifest stating every key the schema offers was refused:\n" + result.Describe());
    }

    /// <summary>
    /// Both keys are required, so omitting either is refused — which is what <c>required</c> says.
    /// </summary>
    [Theory]
    [InlineData("version")]
    [InlineData("members")]
    public void A_key_the_schema_requires_is_one_the_resolver_requires(string key)
    {
        Assert.Contains(key, Required());

        string manifest = string.Join(
            '\n',
            SourcePackage.Manifest(Members)
                .Split('\n')
                .Where(line => !line.StartsWith(key, StringComparison.Ordinal)));

        RulesetSourceResult result = Resolve(manifest);

        Assert.False(result.Ok, $"a manifest with no {key} was accepted, so `required` overstates it.");
    }

    /// <summary>
    /// 🔴 <b>The schema is CLOSED and the resolver is the reason it may be.</b>
    /// </summary>
    /// <remarks>
    /// The Ruleset schema leaves unknown keys permitted, because its surface is bounded by the
    /// sections the shipped files happen to declare and a key no file demonstrates is invisible to
    /// it. ⚠ <b>That argument does not transfer.</b> <c>[source]</c> accepts exactly these two and
    /// refuses the rest by name, so there is no larger set the schema could be an incomplete view
    /// of — and closing it is what makes an editor catch the typo in the one file a designer has no
    /// other feedback on.
    /// </remarks>
    [Fact]
    public void A_key_the_schema_forbids_is_one_the_resolver_refuses()
    {
        Assert.False(
            Source().GetProperty("additionalProperties").GetBoolean(),
            "the schema permits unknown keys in [source]. The resolver refuses them by name, so "
                + "this would advertise a key that always refuses the package.");

        RulesetSourceResult result = Resolve(SourcePackage.Manifest(Members) + "root = \"city.toml\"\n");

        Assert.False(result.Ok, "[source] accepted an unknown key, so the schema must stop forbidding it.");
        Assert.Contains(result.Diagnostics, d => d.Code == RulesetDiagnosticCode.Manifest);
    }

    /// <summary>The pinned version is the one this build reads, and another is refused.</summary>
    [Fact]
    public void The_schema_pins_the_source_version_the_resolver_reads()
    {
        Assert.Equal(
            (int)RulesetCapture.SourceVersion,
            Source().GetProperty("properties").GetProperty("version").GetProperty("const").GetInt32());

        RulesetSourceResult result = Resolve(
            SourcePackage.Manifest(Members).Replace("version = 1", "version = 2", StringComparison.Ordinal));

        Assert.False(result.Ok, "a version this build does not read was accepted.");
        Assert.Contains(result.Diagnostics, d => d.Code == RulesetDiagnosticCode.Version);
    }

    /// <summary>The advertised member ceiling is the one the resolver enforces.</summary>
    /// <remarks>
    /// ⚠ <b>The limit itself is not exercised here.</b> Refusing 257 members means writing 257 files
    /// into a capture to watch one diagnostic fire, which buys a number this test already reads off
    /// the constant. <c>SourceLimitTests</c> owns the refusal; this owns only the agreement.
    /// </remarks>
    [Fact]
    public void The_schema_pins_the_member_limit_the_resolver_enforces() =>
        Assert.Equal(
            RulesetCapture.MemberLimit,
            Source().GetProperty("properties").GetProperty("members").GetProperty("maxItems").GetInt32());

    /// <summary>
    /// 🔴 <b>The manifest rule sits BELOW the member rule, because Taplo applies the last match.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>rulesets/*/*.toml</c> and <c>rulesets/*/ruleset.toml</c> both match a manifest, and Taplo
    /// resolves the overlap by position rather than by specificity. ***Swapping the two blocks gives
    /// every manifest the Ruleset schema and reports nothing***, which is the same failure mode
    /// <c>.vscode/settings.json</c> had and the reason that file was abandoned.
    /// </para>
    /// <para>
    /// ⚠ <b>This asserts the wiring, not the behaviour.</b> That Taplo loads both schemas and applies
    /// each to the right file was verified out of band with the <c>taplo</c> CLI, against a manifest
    /// holding an unknown key and a member holding <c>houses = "yes"</c>. Making that a test would
    /// put a network fetch and a Node toolchain in the suite.
    /// </para>
    /// </remarks>
    [Fact]
    public void The_manifest_schema_is_associated_after_the_member_rule()
    {
        string config = Path.Combine(RepoRoot(), ".taplo.toml");
        string text = File.ReadAllText(config);

        Assert.Contains("rulesets/*/*.toml", text, StringComparison.Ordinal);

        int member = text.LastIndexOf("rulesets/ruleset.schema.json", StringComparison.Ordinal);
        int manifest = text.LastIndexOf(SchemaFile, StringComparison.Ordinal);

        Assert.True(manifest > 0, $"{config} names no manifest schema, so a manifest opens completed "
            + "against the Ruleset schema and shows an error on every section it may not hold.");

        Assert.True(
            manifest > member,
            "the manifest rule is above the member rule in .taplo.toml. Taplo applies the LAST "
                + "matching rule, so in that order every manifest gets the Ruleset schema.");
    }

    private static readonly string[] Members = ["dwelling.toml", "goods.toml", "rules.toml"];

    private static RulesetSourceResult Resolve(string manifest) =>
        RulesetSource.Resolve(SourcePackage.Capture(manifest, SourcePackage.Base));

    private static JsonElement Source()
    {
        string path = Path.Combine(RepoRoot(), SchemaFile);

        Assert.True(File.Exists(path), $"{path} is not there, so a package manifest has no schema.");

        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));

        return document.RootElement.GetProperty("properties").GetProperty("source").Clone();
    }

    private static IReadOnlyList<string> Offered() =>
        [.. Source().GetProperty("properties").EnumerateObject().Select(p => p.Name)];

    private static IReadOnlyList<string> Required() =>
        [.. Source().GetProperty("required").EnumerateArray().Select(e => e.GetString()!)];

    private static string RepoRoot()
    {
        DirectoryInfo? at = new(AppContext.BaseDirectory);

        while (at is not null && !File.Exists(Path.Combine(at.FullName, "CLAUDE.md")))
        {
            at = at.Parent;
        }

        Assert.NotNull(at);

        return at!.FullName;
    }
}
