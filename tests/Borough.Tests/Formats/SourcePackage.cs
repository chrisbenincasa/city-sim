using System.Text;
using Borough.Core.Rules;
using Borough.Formats;

namespace Borough.Tests.Formats;

/// <summary>A small source v1 package held in memory, and helpers for resolving variations of it.</summary>
internal static class SourcePackage
{
    /// <summary>Two Goods, one of them labelled.</summary>
    public const string Goods = """
        [[resource]]
        id = "sundries"
        label = "Sundries"
        family = "good"

        [[resource]]
        id = "repairs"
        family = "good"
        """;

    /// <summary>A dwelling whose Bins refer forward to <see cref="Goods"/>, which sorts after it.</summary>
    public const string Dwelling = """
        [[building]]
        id = "dwelling"
        label = "Terraced house"
        houses = true
        premises = true
        bins = [
          { resource = "sundries", capacity = 48, owner = "occupant" },
          { resource = "repairs",  capacity = 4 },
        ]
        """;

    /// <summary>Two Rules declared out of id order.</summary>
    public const string Consumption = """
        [[rule]]
        id      = "upkeep"
        kind    = "dwelling"
        rate    = 512
        apply   = { min = 1, max = 1 }
        inputs  = [ { scope = "local", resource = "repairs", amount = 1 } ]
        outputs = []

        [[rule]]
        id      = "consume"
        kind    = "dwelling"
        rate    = 64
        apply   = { min = 1, max = 1 }
        inputs  = [ { scope = "local", resource = "sundries", amount = 1 } ]
        outputs = []
        """;

    public static (string Path, string Text)[] Base =>
        [("dwelling.toml", Dwelling), ("goods.toml", Goods), ("rules.toml", Consumption)];

    public static byte[] Utf8(string text) => Encoding.UTF8.GetBytes(text);

    public static string Manifest(params string[] members) =>
        "[source]\nversion = 1\nmembers = ["
        + string.Join(", ", members.Select(member => $"\"{member}\""))
        + "]\n";

    public static RulesetCaptureResult Capture(string manifest, params (string Path, string Text)[] members) =>
        RulesetCapture.FromEntries(
            "ruleset.toml",
            Utf8(manifest),
            members.Select(member => new KeyValuePair<string, byte[]>(member.Path, Utf8(member.Text))));

    public static RulesetSourceResult Resolve(params (string Path, string Text)[] members) =>
        RulesetSource.Resolve(Capture(Manifest([.. members.Select(member => member.Path)]), members));

    public static RulesetSourceResult Accepted(params (string Path, string Text)[] members)
    {
        RulesetSourceResult result = Resolve(members);

        Assert.True(result.Ok, result.Describe());

        return result;
    }

    public static RulesetDiagnostic Refused(string code, params (string Path, string Text)[] members)
    {
        RulesetSourceResult result = Resolve(members);

        Assert.False(result.Ok, "the package was accepted.");

        return Assert.Single(result.Diagnostics, diagnostic => diagnostic.Code == code);
    }

    /// <summary>Asserts two Rulesets hold the same stored values, compared field by field.</summary>
    public static void AssertSameFields(Ruleset? expected, Ruleset? actual) =>
        Assert.Equal(Rendered(expected), Rendered(actual));

    private static List<string> Rendered(Ruleset? ruleset) => ruleset is null
        ? ["null"]
        : [.. RulesetFields.Of(ruleset).Select(f => $"{f.Path} = {f.Value}")];
}
