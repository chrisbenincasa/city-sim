using System.Text.Json;
using Borough.Appearance;

namespace Borough.Tests.Appearance;

/// <summary>The hand-written Style Preset schema offers exactly the keys the reader accepts.</summary>
public sealed class StylePresetSchemaTests
{
    [Theory]
    [InlineData("preset", "[preset]\nname = \"x\"\nera_days = 1\nzz = 1\n")]
    [InlineData("family", "[preset]\nname = \"x\"\nera_days = 1\n[[family]]\nid = \"a\"\nkinds = [\"dwelling\"]\nzz = 1\n")]
    public void The_schema_offers_the_keys_the_reader_accepts(string table, string text)
    {
        AppearanceDiagnostic refusal = Assert.Single(StylePresetReader.Read([("x.toml", text)]).Errors);
        string[] accepted = [.. refusal.Message[(refusal.Message.IndexOf("Expected one of: ", StringComparison.Ordinal) + 17)..]
            .TrimEnd('.').Split(", ").Order(StringComparer.Ordinal)];

        Assert.Equal(accepted, Offered(table));
    }

    private static string[] Offered(string table)
    {
        using JsonDocument schema = JsonDocument.Parse(File.ReadAllText(Path.Combine(RepoRoot(), "appearance", "appearance.schema.json")));
        JsonElement properties = schema.RootElement.GetProperty("properties").GetProperty(table);
        if (table == "family") properties = properties.GetProperty("items");
        return [.. properties.GetProperty("properties").EnumerateObject().Select(p => p.Name).Order(StringComparer.Ordinal)];
    }

    private static string RepoRoot()
    {
        DirectoryInfo? at = new(AppContext.BaseDirectory);
        while (at is not null && !File.Exists(Path.Combine(at.FullName, "CLAUDE.md"))) at = at.Parent;
        Assert.NotNull(at);
        return at!.FullName;
    }
}
