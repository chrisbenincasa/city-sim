using System.Text;
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

    /// <summary>Asserts two object graphs hold the same field values, compared field by field.</summary>
    /// <remarks>
    /// <c>Assert.Equivalent</c> walks public properties, and a Q16.16 value's properties return the
    /// same type, so it never terminates on a Ruleset. Fields are the stored state.
    /// </remarks>
    public static void AssertSameFields(object? expected, object? actual) =>
        Assert.Equal(Fields(expected), Fields(actual));

    private static List<string> Fields(object? root)
    {
        var lines = new List<string>();
        Walk(root, "ruleset", lines, new HashSet<object>(ReferenceEqualityComparer.Instance));
        return lines;
    }

    private static void Walk(object? value, string path, List<string> lines, HashSet<object> seen)
    {
        if (value is null)
        {
            lines.Add($"{path} = null");
            return;
        }

        Type type = value.GetType();

        if (type.IsPrimitive || type.IsEnum || value is string or decimal)
        {
            lines.Add(FormattableString.Invariant($"{path} = {value}"));
            return;
        }

        if (value is Delegate or System.Reflection.MemberInfo or System.Reflection.Pointer)
        {
            lines.Add($"{path} : {type.Name}");
            return;
        }

        if (!type.IsValueType && !seen.Add(value))
        {
            lines.Add($"{path} = (seen)");
            return;
        }

        if (value is Array array)
        {
            lines.Add($"{path}.Length = {array.Length}");
            int index = 0;

            foreach (object? element in array)
            {
                Walk(element, $"{path}[{index++}]", lines, seen);
            }

            return;
        }

        const System.Reflection.BindingFlags Declared = System.Reflection.BindingFlags.Instance
            | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic
            | System.Reflection.BindingFlags.DeclaredOnly;

        for (Type? level = type; level is not null && level != typeof(object) && level != typeof(ValueType);
            level = level.BaseType)
        {
            foreach (System.Reflection.FieldInfo field in level.GetFields(Declared))
            {
                Walk(field.GetValue(value), $"{path}.{field.Name}", lines, seen);
            }
        }
    }
}
