using System.Buffers.Binary;
using System.Text;
using Borough.Core.Determinism;
using Borough.Formats;
using static Borough.Tests.Formats.SourcePackage;

namespace Borough.Tests.Formats;

/// <summary>
/// Plan 0077's manifest capture: explicit membership, portable paths and framed bundle identity.
/// </summary>
public sealed class RulesetCaptureTests
{
    [Fact]
    public void The_bundle_identity_is_the_documented_frame()
    {
        string manifest = Manifest("goods.toml", "dwelling.toml", "rules.toml");
        RulesetCapture capture = Capture(manifest, Base).Capture!;

        var frame = new List<byte>("Borough.RulesetBundle\0"u8.ToArray());
        AddUInt32(frame, 1);
        AddUInt32(frame, 1);
        AddUInt32(frame, 3);
        AddBytes(frame, Utf8(manifest));

        foreach ((string path, string text) in Base)
        {
            AddBytes(frame, Utf8(path));
            AddBytes(frame, Utf8(text));
        }

        Assert.Equal(ContentHash.Of([.. frame]), capture.ContentHash);
        Assert.Equal(["dwelling.toml", "goods.toml", "rules.toml"], capture.Members.Select(m => m.Path));
    }

    [Fact]
    public void Line_endings_are_not_bundle_content_and_comments_are()
    {
        string manifest = Manifest("dwelling.toml", "goods.toml", "rules.toml");
        ulong plain = Capture(manifest, Base).Capture!.ContentHash;

        (string, string)[] crlf = [.. Base.Select(m => (m.Path, m.Text.Replace("\n", "\r\n", StringComparison.Ordinal)))];
        (string, string)[] commented = [.. Base.Select(m => (m.Path, "# note\n" + m.Text))];

        Assert.Equal(plain, Capture(manifest.Replace("\n", "\r\n", StringComparison.Ordinal), crlf).Capture!.ContentHash);
        Assert.NotEqual(plain, Capture(manifest, commented).Capture!.ContentHash);
    }

    [Fact]
    public void Captured_bytes_are_retained_exactly()
    {
        byte[] withCarriageReturns = Utf8(Goods.Replace("\n", "\r\n", StringComparison.Ordinal));
        byte[] entry = Utf8(Manifest("goods.toml"));

        RulesetCapture capture = RulesetCapture.FromEntries(
            "ruleset.toml", entry, [new KeyValuePair<string, byte[]>("goods.toml", withCarriageReturns)]).Capture!;

        Assert.Equal(withCarriageReturns, capture.Members[0].Content.ToArray());
        Assert.Equal(entry, capture.Entry.ToArray());
    }

    [Fact]
    public void A_caller_cannot_replace_a_captured_member()
    {
        RulesetCapture capture = Capture(Manifest("goods.toml"), ("goods.toml", Goods)).Capture!;

        Assert.False(capture.Members is RulesetMember[]);
        Assert.Throws<NotSupportedException>(() =>
            ((IList<RulesetMember>)capture.Members)[0] = capture.Members[0]);
        Assert.Equal("goods.toml", capture.Members[0].Path);
    }

    [Theory]
    [InlineData("Goods.toml")]
    [InlineData("../goods.toml")]
    [InlineData("./goods.toml")]
    [InlineData("data//goods.toml")]
    [InlineData("/goods.toml")]
    [InlineData("data\\\\goods.toml")]
    [InlineData("c:/goods.toml")]
    [InlineData("con.toml")]
    [InlineData("data/lpt1.goods.toml")]
    [InlineData("data./goods.toml")]
    [InlineData("goods.txt")]
    [InlineData("épicerie.toml")]
    [InlineData("two words.toml")]
    [InlineData("")]
    public void A_member_path_outside_the_portable_vocabulary_is_refused(string path)
    {
        RulesetCaptureResult captured = Capture(Manifest(path), (path, Goods));

        Assert.False(captured.Ok);
        RulesetDiagnostic refused = Assert.Single(captured.Diagnostics);
        Assert.Equal((RulesetDiagnosticCode.MemberPath, "ruleset.toml", 3), (refused.Code, refused.Path, refused.Line));
    }

    [Theory]
    [InlineData("goods.toml")]
    [InlineData("economy/goods-2_final.v1.toml")]
    [InlineData("consoles.toml")]
    public void A_portable_member_path_is_accepted(string path)
    {
        Assert.True(Capture(Manifest(path), (path, Goods)).Ok);
    }

    [Fact]
    public void A_manifest_cannot_list_itself_or_a_member_twice()
    {
        RulesetCaptureResult self = Capture(Manifest("ruleset.toml"), ("ruleset.toml", Goods));
        RulesetCaptureResult twice = Capture(Manifest("goods.toml", "goods.toml"), ("goods.toml", Goods));

        Assert.Equal(RulesetDiagnosticCode.MemberPath, Assert.Single(self.Diagnostics).Code);
        Assert.Equal(RulesetDiagnosticCode.MemberDuplicate, Assert.Single(twice.Diagnostics).Code);
    }

    [Fact]
    public void Membership_is_exact()
    {
        RulesetCaptureResult missing = Capture(Manifest("goods.toml", "rules.toml"), ("goods.toml", Goods));
        RulesetCaptureResult unlisted = Capture(Manifest("goods.toml"), ("goods.toml", Goods), ("rules.toml", Consumption));
        RulesetCaptureResult single = Capture("[[resource]]\nname = \"x\"\n", ("goods.toml", Goods));

        RulesetDiagnostic absent = Assert.Single(missing.Diagnostics);
        Assert.Equal((RulesetDiagnosticCode.MemberMissing, 3, 26), (absent.Code, absent.Line, absent.Column));
        Assert.Equal(("rules.toml", RulesetDiagnosticCode.MemberUnlisted),
            (Assert.Single(unlisted.Diagnostics).Path, unlisted.Diagnostics[0].Code));
        Assert.Equal(RulesetDiagnosticCode.MemberUnlisted, Assert.Single(single.Diagnostics).Code);
    }

    [Theory]
    [InlineData("[source]\nversion = 1\nmembers = []\nextra = 1\n", RulesetDiagnosticCode.Manifest)]
    [InlineData("[source]\nmembers = []\n", RulesetDiagnosticCode.Manifest)]
    [InlineData("[source]\nversion = 2\nmembers = []\n", RulesetDiagnosticCode.Version)]
    [InlineData("[source]\nversion = \"1\"\nmembers = []\n", RulesetDiagnosticCode.Manifest)]
    [InlineData("[source]\nversion = 1\n", RulesetDiagnosticCode.Manifest)]
    [InlineData("[source]\nversion = 1\nmembers = \"goods.toml\"\n", RulesetDiagnosticCode.Manifest)]
    [InlineData("[source]\nversion = 1\nmembers = [1]\n", RulesetDiagnosticCode.Manifest)]
    [InlineData("[source]\nversion = 1\nmembers = []\n\n[[resource]]\nname = \"x\"\n", RulesetDiagnosticCode.Manifest)]
    [InlineData("rate = 3\n[source]\nversion = 1\nmembers = []\n", RulesetDiagnosticCode.Manifest)]
    [InlineData("[[source]]\nversion = 1\nmembers = []\n", RulesetDiagnosticCode.Manifest)]
    [InlineData("source = { version = 1, members = [] }\n", RulesetDiagnosticCode.Manifest)]
    [InlineData("[source]\nversion = 1\nmembers = [\n", RulesetDiagnosticCode.Syntax)]
    public void A_malformed_source_table_is_refused_and_never_read_as_a_single_file(string manifest, string code)
    {
        RulesetCaptureResult captured = Capture(manifest);

        Assert.False(captured.Ok);
        Assert.Contains(captured.Diagnostics, d => d.Code == code && d.Path == "ruleset.toml");
    }

    [Fact]
    public void A_manifest_with_no_members_captures()
    {
        RulesetCaptureResult captured = Capture(Manifest());

        Assert.True(captured.Ok);
        Assert.Empty(captured.Capture!.Members);
    }

    [Fact]
    public void Source_bytes_must_be_utf8_and_may_start_with_a_byte_order_mark()
    {
        byte[] bom = [.. Encoding.UTF8.Preamble];
        byte[] invalid = [.. Utf8(Goods), 0xFF];

        RulesetCaptureResult marked = RulesetCapture.FromEntries(
            "ruleset.toml", [.. bom, .. Utf8(Manifest("goods.toml"))],
            [new KeyValuePair<string, byte[]>("goods.toml", [.. bom, .. Utf8(Goods)])]);

        RulesetCaptureResult badMember = RulesetCapture.FromEntries(
            "ruleset.toml", Utf8(Manifest("goods.toml")), [new KeyValuePair<string, byte[]>("goods.toml", invalid)]);

        RulesetCaptureResult badManifest = RulesetCapture.FromEntries(
            "ruleset.toml", [.. Utf8(Manifest() + "# "), 0xFF, (byte)'\n'], []);

        Assert.True(RulesetSource.Resolve(marked).Ok, RulesetSource.Resolve(marked).Describe());
        Assert.False(badMember.Ok);
        Assert.Equal(("goods.toml", RulesetDiagnosticCode.Utf8), (badMember.Diagnostics[0].Path, badMember.Diagnostics[0].Code));
        Assert.Equal(RulesetDiagnosticCode.Utf8, Assert.Single(badManifest.Diagnostics).Code);
    }

    [Fact]
    public void A_package_loads_from_a_directory_without_depending_on_where_it_is()
    {
        using var first = new Directory();
        using var second = new Directory();

        foreach (Directory package in (Directory[])[first, second])
        {
            package.Write("ruleset.toml", Manifest("buildings/dwelling.toml", "goods.toml", "rules.toml"));
            package.Write("buildings/dwelling.toml", Dwelling);
            package.Write("goods.toml", Goods);
            package.Write("rules.toml", Consumption);
        }

        RulesetSourceResult here = RulesetSource.Load(Path.Combine(first.Root, "ruleset.toml"));
        RulesetSourceResult there = RulesetSource.Load(Path.Combine(second.Root, "ruleset.toml"));

        Assert.True(here.Ok, here.Describe());
        Assert.Equal(here.Capture!.ContentHash, there.Capture!.ContentHash);
    }

    [Fact]
    public void A_member_must_be_a_regular_file_reached_without_links()
    {
        using var package = new Directory();

        package.Write("ruleset.toml", Manifest("linked.toml", "through/goods.toml", "folder.toml", "absent.toml"));
        string real = package.Write("real/goods.toml", Goods);
        File.CreateSymbolicLink(Path.Combine(package.Root, "linked.toml"), real);
        System.IO.Directory.CreateSymbolicLink(Path.Combine(package.Root, "through"), Path.GetDirectoryName(real)!);
        System.IO.Directory.CreateDirectory(Path.Combine(package.Root, "folder.toml"));

        RulesetCaptureResult captured = RulesetCapture.Read(Path.Combine(package.Root, "ruleset.toml"));

        Assert.Equal(
            [RulesetDiagnosticCode.MemberRead, RulesetDiagnosticCode.MemberRead,
             RulesetDiagnosticCode.MemberRead, RulesetDiagnosticCode.MemberMissing],
            captured.Diagnostics.Select(d => d.Code));
        Assert.Contains("symbolic link", captured.Diagnostics[0].Reason, StringComparison.Ordinal);
        Assert.Contains("directory", captured.Diagnostics[2].Reason, StringComparison.Ordinal);
    }

    private static void AddUInt32(List<byte> frame, uint value)
    {
        byte[] bytes = new byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(bytes, value);
        frame.AddRange(bytes);
    }

    private static void AddBytes(List<byte> frame, byte[] value)
    {
        byte[] length = new byte[8];
        BinaryPrimitives.WriteUInt64LittleEndian(length, (ulong)value.Length);
        frame.AddRange(length);
        frame.AddRange(value);
    }

    private sealed class Directory : IDisposable
    {
        public string Root { get; } = System.IO.Directory.CreateTempSubdirectory("borough-source-").FullName;

        public string Write(string relative, string text)
        {
            string path = Path.Combine(Root, relative);
            System.IO.Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, text);
            return path;
        }

        public void Dispose() => System.IO.Directory.Delete(Root, recursive: true);
    }
}
