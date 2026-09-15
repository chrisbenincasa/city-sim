using System.Text;
using Borough.Formats;

namespace Borough.Tests.Formats;

/// <summary>
/// Plan 0077's compatibility boundary: an entry without <c>[source]</c> keeps its single-file
/// reading, content hash and declaration order.
/// </summary>
public sealed class RulesetSourceLegacyTests
{
    public static TheoryData<string> ShippedRulesets()
    {
        var files = new TheoryData<string>();

        foreach (string file in Directory.GetFiles(
            Path.Combine(AppContext.BaseDirectory, "Rulesets"), "*.toml").Order(StringComparer.Ordinal))
        {
            files.Add(Path.GetFileName(file));
        }

        return files;
    }

    [Theory]
    [MemberData(nameof(ShippedRulesets))]
    public void A_shipped_ruleset_resolves_exactly_as_the_single_file_reader_reads_it(string name)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Rulesets", name);

        RulesetSourceResult source = RulesetSource.Load(path);
        RulesetLoadResult legacy = RulesetLoader.Load(path);

        Assert.Equal(RulesetSourceMode.Legacy, source.Capture!.Mode);
        Assert.Equal(RulesetFile.HashOf(path), source.Capture.ContentHash);
        Assert.Equal(legacy.Describe(), source.ToLoadResult().Describe());
        SourcePackage.AssertSameFields(legacy.Ruleset, source.Ruleset);
        Assert.Empty(source.Declarations);
    }

    [Fact]
    public void A_single_file_keeps_its_hash_decoding_and_refusals()
    {
        const string Text = "\r\n[[resource]]\r\nname = \"money\"\r\nfamily = \"moolah\"\r\n";
        byte[] bytes = [.. Encoding.UTF8.Preamble, .. Encoding.UTF8.GetBytes(Text)];

        RulesetSourceResult source = RulesetSource.Resolve(RulesetCapture.FromEntries("single.toml", bytes, []));
        RulesetLoadResult legacy = RulesetLoader.Parse(Text, "single.toml");

        Assert.Equal(RulesetFile.HashOfContent(bytes), source.Capture!.ContentHash);
        Assert.False(source.Ok);
        Assert.Equal(legacy.Describe(), source.ToLoadResult().Describe());
        Assert.Equal(legacy.Refusals.Count, source.Diagnostics.Count);
    }

    /// <summary>
    /// The source reader lowers <c>id</c> into the single-file reader's identity <c>name</c> except
    /// for sections whose <c>name</c> is not an identity. This keeps that list in step with the reader.
    /// </summary>
    [Fact]
    public void Only_terrain_hinterland_and_lattice_arrays_lack_an_identity_name()
    {
        var unnamed = new SortedSet<string>(StringComparer.Ordinal);

        foreach (string file in Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "Rulesets"), "*.toml"))
        {
            foreach ((string section, IReadOnlyDictionary<string, RulesetKeyKind> keys) in
                RulesetLoader.KeySurface(File.ReadAllText(file), Path.GetFileName(file)))
            {
                if (section.StartsWith("[[", StringComparison.Ordinal)
                    && !section.Contains('.', StringComparison.Ordinal)
                    && section is not ("[[rule]] inputs" or "[[rule]] outputs")
                    && !section.Contains(' ', StringComparison.Ordinal)
                    && !keys.ContainsKey("name"))
                {
                    unnamed.Add(section);
                }
            }
        }

        Assert.Equal(["[[hinterland]]", "[[lattice]]"], unnamed);
    }
}
