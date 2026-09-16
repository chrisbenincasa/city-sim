using System.Text;
using Borough.Formats;
using Borough.Shell;
using static Borough.Tests.Formats.SourcePackage;

namespace Borough.Tests.Shell;

/// <summary>
/// Plan 0077's shell tuner over a captured Ruleset: the dials reach a package's members.
/// </summary>
public sealed class CityTuningTests
{
    private const string Roads = """
        [roads]
        block_tiles = 12
        arterial_count = 2
        """;

    private const string Lots = """
        [lots]
        lots_per_segment = 4
        """;

    private static RulesetCapture Package(params (string Path, string Text)[] members) =>
        Capture(Manifest([.. members.Select(member => member.Path)]), members).Capture!;

    private static string Text(RulesetCapture capture, string path) =>
        Encoding.UTF8.GetString(
            capture.Members.Single(member => member.Path == path).Content.Span);

    [Fact]
    public void A_dial_reaches_the_member_that_states_it_and_leaves_the_others_alone()
    {
        RulesetCapture before = Package(("roads.toml", Roads), ("lots.toml", Lots));

        RulesetCapture after = CityTuning.Turned(before, [("[roads]", "block_tiles", "20")]).Capture!;

        Assert.Contains("block_tiles = 20", Text(after, "roads.toml"));
        Assert.Contains("arterial_count = 2", Text(after, "roads.toml"));
        Assert.Equal(Lots, Text(after, "lots.toml"));
        Assert.NotEqual(before.ContentHash, after.ContentHash);
    }

    [Fact]
    public void Reading_a_value_finds_it_in_whichever_member_states_it()
    {
        RulesetCapture capture = Package(("roads.toml", Roads), ("lots.toml", Lots));

        Assert.Equal("12", CityTuning.Stated(capture, "[roads]", "block_tiles"));
        Assert.Equal("4", CityTuning.Stated(capture, "[lots]", "lots_per_segment"));
        Assert.Null(CityTuning.Stated(capture, "[placement]", "candidates"));
    }

    [Fact]
    public void A_dial_no_member_states_leaves_the_identity_where_it_was()
    {
        RulesetCapture before = Package(("roads.toml", Roads), ("lots.toml", Lots));

        RulesetCapture after =
            CityTuning.Turned(before, [("[placement]", "candidates", "9")]).Capture!;

        Assert.Equal(before.ContentHash, after.ContentHash);
    }

    [Fact]
    public void A_byte_order_mark_and_the_line_endings_survive_a_turn()
    {
        byte[] marked = [.. Encoding.UTF8.Preamble, .. Utf8(Roads.ReplaceLineEndings("\r\n"))];
        RulesetCapture before = RulesetCapture.FromEntries(
            "ruleset.toml",
            Utf8(Manifest("roads.toml")),
            [new KeyValuePair<string, byte[]>("roads.toml", marked)]).Capture!;

        RulesetCapture after = CityTuning.Turned(before, [("[roads]", "block_tiles", "20")]).Capture!;
        byte[] content = after.Members.Single().Content.ToArray();

        Assert.Equal(Encoding.UTF8.Preamble.ToArray(), content[..Encoding.UTF8.Preamble.Length]);
        Assert.Contains("block_tiles = 20\r\n", Encoding.UTF8.GetString(content));
        Assert.DoesNotContain("\n\n", Encoding.UTF8.GetString(content).Replace("\r\n", "\n"));
    }

    [Fact]
    public void A_tuned_package_is_written_out_as_a_directory_a_runner_can_load()
    {
        RulesetCapture capture =
            CityTuning.Turned(Package(Base), [("[roads]", "block_tiles", "20")]).Capture!;
        string folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            string entry = CityTuning.WriteBeside(capture, Path.Combine(folder, "session-0.borough-log"));

            RulesetCaptureResult reread = RulesetCapture.Read(entry);

            Assert.True(reread.Ok, reread.Ok ? string.Empty : reread.Diagnostics[0].ToString());
            Assert.Equal(capture.ContentHash, reread.Capture!.ContentHash);
            Assert.True(RulesetSource.Resolve(reread).Ok);
        }
        finally { Directory.Delete(folder, true); }
    }

    [Fact]
    public void A_single_file_is_written_out_as_one_file()
    {
        RulesetCapture capture = RulesetCapture.FromEntries("base.toml", Utf8(Goods), []).Capture!;
        string folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            string entry = CityTuning.WriteBeside(capture, Path.Combine(folder, "session-0.borough-log"));

            Assert.Equal(Path.Combine(folder, "session-0.toml"), entry);
            Assert.Equal(capture.ContentHash, RulesetCapture.Read(entry).Capture!.ContentHash);
        }
        finally { Directory.Delete(folder, true); }
    }
}
