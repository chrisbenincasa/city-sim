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
    public void A_manifest_cannot_list_a_member_twice()
    {
        RulesetCaptureResult twice = Capture(Manifest("goods.toml", "goods.toml"), ("goods.toml", Goods));

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

    /// <summary>
    /// <c>["source"]</c> is the same table as <c>[source]</c>, so it captures the same package.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>A manifest read as a single file loads NOTHING and says nothing.</b> Every member would
    /// be ignored and the entry would reach the reader alone, where <c>version</c> and
    /// <c>members</c> refuse as unknown keys — a refusal that describes neither the mistake nor the
    /// package. Key names are therefore resolved to their TOML spelling rather than compared as
    /// written.
    /// </remarks>
    [Theory]
    [InlineData("[\"source\"]")]
    [InlineData("[ source ]")]
    [InlineData("['source']")]
    public void A_quoted_source_table_is_the_same_manifest(string header)
    {
        string manifest = Manifest("goods.toml", "dwelling.toml", "rules.toml");

        RulesetCapture quoted =
            Capture(manifest.Replace("[source]", header, StringComparison.Ordinal), Base).Capture!;

        Assert.Equal(RulesetSourceMode.Source, quoted.Mode);
        Assert.Equal(3, quoted.Members.Count);
    }

    /// <summary>
    /// A bundle's bytes are its identity, so a different spelling of one header is a different
    /// Ruleset even though it resolves to the same one.
    /// </summary>
    [Fact]
    public void A_quoted_source_table_is_not_the_same_bytes()
    {
        string manifest = Manifest("goods.toml", "dwelling.toml", "rules.toml");

        Assert.NotEqual(
            Capture(manifest, Base).Capture!.ContentHash,
            Capture(manifest.Replace("[source]", "[\"source\"]", StringComparison.Ordinal), Base)
                .Capture!.ContentHash);
    }

    /// <summary>
    /// Invalid bytes are refused before the mode is chosen, because the mode is read out of the
    /// decoded text.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>Decoding permissively first would downgrade a manifest to a single file.</b> A bad byte
    /// inside the <c>[source]</c> token becomes a replacement character, the token stops matching,
    /// and the package loads none of its members instead of being refused.
    /// </remarks>
    [Fact]
    public void A_bad_byte_inside_the_source_token_is_refused_rather_than_downgraded()
    {
        byte[] entry = [.. "[sou"u8, 0xFF, .. "rce]\nversion = 1\nmembers = []\n"u8];

        RulesetCaptureResult captured = RulesetCapture.FromEntries("ruleset.toml", entry, []);

        Assert.False(captured.Ok);
        Assert.Equal(RulesetDiagnosticCode.Utf8, Assert.Single(captured.Diagnostics).Code);
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

    [Fact]
    public void A_manifest_or_member_beyond_the_loading_limits_is_refused()
    {
        string oversize = new('#', RulesetCapture.ByteLimit + 1);
        string[] many = [.. Enumerable.Range(0, RulesetCapture.MemberLimit + 1).Select(i => $"m{i}.toml")];

        RulesetCaptureResult member = Capture(Manifest("goods.toml"), ("goods.toml", oversize));
        RulesetCaptureResult manifest = Capture(Manifest() + oversize);
        RulesetCaptureResult crowded = Capture(Manifest(many), [.. many.Select(path => (path, Goods))]);

        Assert.Equal(RulesetDiagnosticCode.Limit, Assert.Single(member.Diagnostics).Code);
        Assert.Equal(RulesetDiagnosticCode.Limit, Assert.Single(manifest.Diagnostics).Code);
        Assert.Equal(RulesetDiagnosticCode.Limit, Assert.Single(crowded.Diagnostics).Code);
    }

    /// <summary>
    /// Members share the manifest's directory, so listing it would read it. A bundle supplies the
    /// manifest outside the member map, where a member may carry the same name.
    /// </summary>
    [Fact]
    public void A_manifest_cannot_list_itself()
    {
        using var package = new Directory();

        package.Write("ruleset.toml", Manifest("ruleset.toml"));

        RulesetCaptureResult captured = RulesetCapture.Read(Path.Combine(package.Root, "ruleset.toml"));

        Assert.Equal(RulesetDiagnosticCode.MemberPath, Assert.Single(captured.Diagnostics).Code);
    }

    [Fact]
    public void An_oversize_member_on_disk_is_refused()
    {
        using var package = new Directory();

        package.Write("ruleset.toml", Manifest("goods.toml"));
        package.Write("goods.toml", new string('#', RulesetCapture.ByteLimit + 1));

        RulesetCaptureResult captured = RulesetCapture.Read(Path.Combine(package.Root, "ruleset.toml"));

        Assert.Equal(RulesetDiagnosticCode.Limit, Assert.Single(captured.Diagnostics).Code);
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

    /// <summary>
    /// A capture is read from the entry once, and everything downstream works from those bytes.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>Reading the path a second time would let an edit between the two reads register one
    /// content identity against different Rules</b>, which a save header and a replay transition
    /// both read as provenance. Rewriting the files under a capture that already exists is the
    /// sharpest way to state it.
    /// </remarks>
    [Fact]
    public void A_capture_resolves_from_its_own_bytes_and_never_from_the_path_again()
    {
        using var directory = new Directory();

        directory.Write("goods.toml", Goods);
        string entry = directory.Write("ruleset.toml", Manifest("goods.toml"));

        RulesetCapture capture = RulesetCapture.Read(entry).Capture!;
        ulong identity = capture.ContentHash;

        directory.Write("goods.toml", Goods + "\n[[resource]]\nid = \"flour\"\nfamily = \"good\"\n");
        directory.Write("ruleset.toml", Manifest("goods.toml") + "\n# edited\n");

        RulesetSourceResult resolved = RulesetSource.Resolve(capture);

        Assert.True(resolved.Ok, resolved.Describe());
        Assert.Equal(identity, capture.ContentHash);
        Assert.DoesNotContain(resolved.Declarations, d => d.Id == "flour");
        Assert.NotEqual(identity, RulesetCapture.Read(entry).Capture!.ContentHash);
    }

    /// <summary>
    /// The member limit is a count, so the accepted and refused sides are one member apart.
    /// </summary>
    [Fact]
    public void The_member_limit_is_read_at_its_own_boundary()
    {
        (string, string)[] members =
            [.. Enumerable.Range(0, RulesetCapture.MemberLimit + 1)
                .Select(i => ($"m{i:0000}.toml", "# nothing\n"))];

        string[] paths = [.. members.Select(member => member.Item1)];

        RulesetCaptureResult full = Capture(Manifest(paths[..RulesetCapture.MemberLimit]),
            members[..RulesetCapture.MemberLimit]);

        RulesetCaptureResult over = Capture(Manifest(paths), members);

        Assert.True(full.Ok, full.Ok ? string.Empty : full.Diagnostics[0].ToString());
        Assert.Equal(RulesetCapture.MemberLimit, full.Capture!.Members.Count);
        Assert.False(over.Ok);
        Assert.Equal(RulesetDiagnosticCode.Limit, over.Diagnostics[0].Code);
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
