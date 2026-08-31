using System.Text.Json;
using Borough.Formats;

namespace Borough.Tests.Corpus;

/// <summary>
/// The committed editor schema is the loader's own key surface, and this is the line that keeps them
/// together.
/// </summary>
/// <remarks>
/// <para>
/// <b>The corpus's second document-to-<em>code</em> check</b>, after <c>RefusalCountTests</c>, and it
/// exists for the same reason: <c>rulesets/ruleset.schema.json</c> is a description of what a Ruleset
/// may say, and the loader is another one. <c>plans/0012</c> <b>Cause 1</b> — ***every document that
/// stores what another document owns drifted, and the only large one that did not stores none.***
/// </para>
/// <para>
/// <b>It compares paths and types, never bytes.</b> A byte comparison would fail on a reformat and
/// say nothing about the city; what moves when somebody adds a reader is the set of keys and the
/// shape each one wants, so that is what is asserted. ⚠ <b>The consequence is that the file's
/// FORMATTING is unguarded</b> — it says <c>do not edit</c> in its own description and this test
/// will not catch somebody who did, so long as they changed no key.
/// </para>
/// <para>
/// ⚠ <b>The surface is bounded by the shipped Rulesets and the schema is bounded by the same
/// thing.</b> A reader only ever asks a table that exists, so a section no file declares is invisible
/// to both sides of this comparison and the test passes over it in silence. That is not a hole this
/// test can close — it is why the schema leaves unknown keys permitted.
/// </para>
/// </remarks>
public sealed class RulesetSchemaTests
{
    /// <summary>
    /// Every key the loader reads is in the committed schema, with the type the loader wants.
    /// </summary>
    [Fact]
    public void The_committed_schema_is_the_loaders_own_key_surface()
    {
        string root = RepoRoot();
        string folder = Path.Combine(root, "rulesets");
        string path = Path.Combine(folder, "ruleset.schema.json");

        Assert.True(
            File.Exists(path),
            $"{path} is not there. Regenerate it with:\n"
                + "  dotnet run --project src/Borough.Headless -- --schema "
                + "--ruleset rulesets/minimal.toml > rulesets/ruleset.schema.json");

        SortedDictionary<string, string> loader = FromLoader(folder);
        SortedDictionary<string, string> committed = FromSchema(path);

        List<string> missing = loader.Keys.Where(k => !committed.ContainsKey(k)).ToList();
        List<string> extra = committed.Keys.Where(k => !loader.ContainsKey(k)).ToList();
        List<string> wrong = loader.Keys
            .Where(k => committed.TryGetValue(k, out string? was) && was != loader[k])
            .Select(k => $"{k}: schema says {committed[k]}, the loader wants {loader[k]}")
            .ToList();

        Assert.True(
            missing.Count == 0 && extra.Count == 0 && wrong.Count == 0,
            "rulesets/ruleset.schema.json has drifted from RulesetLoader.\n"
                + Describe("read by the loader and absent from the schema", missing)
                + Describe("in the schema and read by nothing", extra)
                + Describe("typed differently", wrong)
                + "\nRegenerate rather than editing it:\n"
                + "  dotnet run --project src/Borough.Headless -- --schema "
                + "--ruleset rulesets/minimal.toml > rulesets/ruleset.schema.json");
    }

    /// <summary>
    /// The schema is associated with the Ruleset folder, or an editor never loads it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 ⚠ <b>THE ASSOCIATION IS IN <c>.taplo.toml</c>, AND <c>.vscode/settings.json</c> WAS TRIED
    /// FIRST AND SILENTLY DID NOTHING.</b> <c>evenBetterToml.schema.associations</c> documents
    /// itself as matching <em>absolute document URIs</em> against <em>an absolute URI to the JSON
    /// schema</em> — so a working entry names <c>/home/somebody</c>, which is not a thing a
    /// repository may commit. The editor reported <em>no schema selected</em> and nothing else.
    /// ***A configuration that fails by doing nothing is why this test exists.***
    /// </para>
    /// <para>
    /// ⚠ <b>It is not an <c>#:schema</c> line in each Ruleset either.</b> <c>declining.toml</c> and
    /// <c>congested.toml</c> are golden-baseline artefacts whose recorded <em>content hash</em>
    /// moves when they are edited at all, comments included.
    /// </para>
    /// <para>
    /// ⚠ <b>This asserts the wiring and not the behaviour.</b> That Taplo loads the config, applies
    /// the schema and enforces a type was verified out of band, with the <c>taplo</c> CLI against a
    /// Ruleset with <c>occupants = "four"</c> in it — a check this test cannot make without putting
    /// a network fetch in the suite.
    /// </para>
    /// </remarks>
    [Fact]
    public void The_schema_is_associated_with_the_ruleset_folder()
    {
        string config = Path.Combine(RepoRoot(), ".taplo.toml");

        Assert.True(
            File.Exists(config),
            $"{config} is not there, so no editor finds the schema and every Ruleset opens with "
                + "no completion at all.");

        string text = File.ReadAllText(config);

        Assert.Contains("rulesets/*.toml", text, StringComparison.Ordinal);
        Assert.Contains("rulesets/ruleset.schema.json", text, StringComparison.Ordinal);
    }

    private static string Describe(string what, List<string> names) =>
        names.Count == 0 ? string.Empty : $"\n  {what}:\n    " + string.Join("\n    ", names) + "\n";

    /// <summary>Path to type, for every key any reader asks for, unioned across the folder.</summary>
    private static SortedDictionary<string, string> FromLoader(string folder)
    {
        var flat = new SortedDictionary<string, string>(StringComparer.Ordinal);
        string[] files = Directory.GetFiles(folder, "*.toml");

        Array.Sort(files, StringComparer.Ordinal);

        foreach (string file in files)
        {
            IReadOnlyDictionary<string, IReadOnlyDictionary<string, RulesetKeyKind>> surface =
                RulesetLoader.KeySurface(File.ReadAllText(file), Path.GetFileName(file));

            foreach (KeyValuePair<string, IReadOnlyDictionary<string, RulesetKeyKind>> section in surface)
            {
                string[] steps = section.Key.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                if (steps.Length == 0 || !steps[0].StartsWith('['))
                {
                    continue;
                }

                string context = string.Join('.', steps.Select(s => s.Trim('[', ']')));

                foreach (KeyValuePair<string, RulesetKeyKind> key in section.Value)
                {
                    string at = $"{context}.{key.Key}";

                    // A key typed by one file and untyped by another keeps the type: the schema
                    // generator unions the same way, so the two agree by construction.
                    if (!flat.TryGetValue(at, out string? already) || already == "any")
                    {
                        flat[at] = Named(key.Value);
                    }
                }
            }
        }

        // A key that also heads a section holds a table rather than a scalar, and the schema writes
        // it as the section. Drop the leaf reading so the two sides describe the same thing.
        foreach (string at in flat.Keys.ToList())
        {
            if (flat.Keys.Any(other => other.StartsWith(at + ".", StringComparison.Ordinal)))
            {
                flat.Remove(at);
            }
        }

        return flat;
    }

    private static string Named(RulesetKeyKind kind) => kind switch
    {
        RulesetKeyKind.Whole => "integer",
        RulesetKeyKind.Quoted => "string",
        RulesetKeyKind.Table => "object",
        RulesetKeyKind.Array => "array",
        _ => "any",
    };

    /// <summary>The same path-to-type map, read back out of the committed JSON Schema.</summary>
    private static SortedDictionary<string, string> FromSchema(string path)
    {
        var flat = new SortedDictionary<string, string>(StringComparer.Ordinal);

        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));

        Walk(document.RootElement, string.Empty, flat);

        return flat;
    }

    private static void Walk(JsonElement node, string at, SortedDictionary<string, string> into)
    {
        JsonElement holder = node;

        if (node.TryGetProperty("items", out JsonElement items))
        {
            holder = items;
        }

        if (holder.TryGetProperty("properties", out JsonElement properties))
        {
            foreach (JsonProperty child in properties.EnumerateObject())
            {
                Walk(child.Value, at.Length == 0 ? child.Name : $"{at}.{child.Name}", into);
            }

            return;
        }

        if (at.Length > 0)
        {
            into[at] = node.TryGetProperty("type", out JsonElement type)
                ? type.GetString() ?? "any"
                : "any";
        }
    }

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
