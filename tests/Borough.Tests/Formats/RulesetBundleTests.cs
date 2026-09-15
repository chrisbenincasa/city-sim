using System.Text.Json;
using Borough.Formats;
using static Borough.Tests.Formats.SourcePackage;

namespace Borough.Tests.Formats;

/// <summary>
/// Plan 0077's bundle codec: the stored shape both hosts keep a captured Ruleset in.
/// </summary>
public sealed class RulesetBundleTests
{
    [Fact]
    public void A_package_round_trips_through_the_documented_entries()
    {
        RulesetCapture captured = Capture(Manifest("dwelling.toml", "goods.toml", "rules.toml"), Base).Capture!;

        List<KeyValuePair<string, byte[]>> entries = [.. RulesetBundle.Write(captured)];
        RulesetCaptureResult read = RulesetBundle.Read(entries);

        Assert.Equal(
            ["bundle.json", "source.toml", "members/dwelling.toml", "members/goods.toml", "members/rules.toml"],
            entries.Select(entry => entry.Key));
        Assert.True(read.Ok, read.Capture is null ? read.Diagnostics[0].ToString() : string.Empty);
        Assert.Equal(captured.ContentHash, read.Capture!.ContentHash);
        Assert.Equal(
            captured.Members.Select(member => member.Path), read.Capture.Members.Select(member => member.Path));
        Assert.True(RulesetSource.Resolve(read).Ok);
    }

    [Fact]
    public void The_metadata_records_the_mode_identity_versions_and_sorted_members()
    {
        RulesetCapture captured = Capture(Manifest("rules.toml", "goods.toml", "dwelling.toml"), Base).Capture!;

        JsonElement envelope = Envelope(RulesetBundle.Write(captured));

        Assert.Equal(1u, envelope.GetProperty("Version").GetUInt32());
        Assert.Equal("source", envelope.GetProperty("Mode").GetString());
        Assert.Equal(RulesetBundle.Identity(captured.ContentHash), envelope.GetProperty("Identity").GetString());
        Assert.Equal(1u, envelope.GetProperty("Source").GetUInt32());
        Assert.Equal(1u, envelope.GetProperty("Resolver").GetUInt32());
        Assert.Equal(
            ["dwelling.toml", "goods.toml", "rules.toml"],
            envelope.GetProperty("Members").EnumerateArray().Select(member => member.GetString()!));
    }

    [Fact]
    public void A_single_file_round_trips_under_its_legacy_hash()
    {
        RulesetCapture captured = RulesetCapture.FromEntries("base.toml", Utf8(Goods), []).Capture!;

        List<KeyValuePair<string, byte[]>> entries = [.. RulesetBundle.Write(captured)];
        RulesetCaptureResult read = RulesetBundle.Read(entries);
        JsonElement envelope = Envelope(entries);

        Assert.Equal(["bundle.json", "ruleset.toml"], entries.Select(entry => entry.Key));
        Assert.Equal("legacy", envelope.GetProperty("Mode").GetString());
        Assert.False(envelope.TryGetProperty("Source", out _));
        Assert.False(envelope.TryGetProperty("Members", out _));
        Assert.Equal(RulesetFile.HashOfContent(Utf8(Goods)), read.Capture!.ContentHash);
        Assert.Equal(RulesetSourceMode.Legacy, read.Capture.Mode);
    }

    [Fact]
    public void The_json_spelling_is_not_an_identity_input()
    {
        List<KeyValuePair<string, byte[]>> entries = Bundled();
        string compact = JsonSerializer.Serialize(JsonDocument.Parse(entries[0].Value).RootElement);

        entries[0] = new KeyValuePair<string, byte[]>("bundle.json", Utf8(compact));

        Assert.True(RulesetBundle.Read(entries).Ok);
    }

    [Fact]
    public void An_edited_member_no_longer_folds_to_the_recorded_identity()
    {
        List<KeyValuePair<string, byte[]>> entries = Bundled();
        int member = entries.FindIndex(entry => entry.Key == "members/goods.toml");

        entries[member] = new KeyValuePair<string, byte[]>(entries[member].Key, Utf8("# edited\n" + Goods));

        Assert.Equal(RulesetDiagnosticCode.BundleIdentity, Refusal(entries).Code);
    }

    [Fact]
    public void A_missing_member_is_refused_and_never_falls_back()
    {
        List<KeyValuePair<string, byte[]>> entries = Bundled();
        entries.RemoveAll(entry => entry.Key == "members/goods.toml");

        Assert.Equal(RulesetDiagnosticCode.MemberMissing, Refusal(entries).Code);
    }

    [Theory]
    [InlineData("bundle.json")]
    [InlineData("source.toml")]
    public void A_bundle_missing_a_required_entry_is_refused(string entry)
    {
        List<KeyValuePair<string, byte[]>> entries = Bundled();
        entries.RemoveAll(held => held.Key == entry);

        Assert.Equal(RulesetDiagnosticCode.Bundle, Refusal(entries).Code);
    }

    [Fact]
    public void An_entry_the_metadata_does_not_declare_is_refused()
    {
        List<KeyValuePair<string, byte[]>> entries = Bundled();
        entries.Add(new KeyValuePair<string, byte[]>("notes.md", Utf8("stray")));

        RulesetDiagnostic refused = Refusal(entries);

        Assert.Equal((RulesetDiagnosticCode.Bundle, "notes.md"), (refused.Code, refused.Path));
    }

    [Fact]
    public void An_entry_supplied_twice_is_refused()
    {
        List<KeyValuePair<string, byte[]>> entries = Bundled();
        entries.Add(entries[0]);

        Assert.Equal(RulesetDiagnosticCode.Bundle, Refusal(entries).Code);
    }

    [Theory]
    [InlineData("""{"Version":2,"Mode":"source","Identity":"0000000000000000"}""", RulesetDiagnosticCode.BundleVersion)]
    [InlineData("""{"Version":1,"Mode":"source","Identity":"0000000000000000","Source":2,"Resolver":1,"Members":[]}""", RulesetDiagnosticCode.BundleVersion)]
    [InlineData("""{"Version":1,"Mode":"source","Identity":"0000000000000000","Source":1,"Resolver":9,"Members":[]}""", RulesetDiagnosticCode.BundleVersion)]
    [InlineData("""{"Version":1,"Mode":"future","Identity":"0000000000000000"}""", RulesetDiagnosticCode.Bundle)]
    [InlineData("""{"Version":1,"Mode":"source","Identity":"0000000000000000","Source":1,"Resolver":1}""", RulesetDiagnosticCode.Bundle)]
    [InlineData("""{"Version":1,"Mode":"legacy","Identity":"0000000000000000","Members":[]}""", RulesetDiagnosticCode.Bundle)]
    [InlineData("""{"Version":1,"Mode":"source","Identity":"NOTAHASH00000000"}""", RulesetDiagnosticCode.Bundle)]
    [InlineData("""{"Version":1,"Mode":"source","Identity":"abc"}""", RulesetDiagnosticCode.Bundle)]
    [InlineData("not json", RulesetDiagnosticCode.Bundle)]
    public void Unreadable_metadata_is_refused(string metadata, string code)
    {
        List<KeyValuePair<string, byte[]>> entries = Bundled();
        entries[0] = new KeyValuePair<string, byte[]>("bundle.json", Utf8(metadata));

        Assert.Equal(code, Refusal(entries).Code);
    }

    [Fact]
    public void A_member_list_disagreeing_with_the_manifest_is_refused()
    {
        List<KeyValuePair<string, byte[]>> entries = Bundled();
        RulesetCapture captured = Capture(Manifest([.. Base.Select(member => member.Path)]), Base).Capture!;

        entries[0] = new KeyValuePair<string, byte[]>("bundle.json", Utf8(
            $$"""
            {"Version":1,"Mode":"source","Identity":"{{RulesetBundle.Identity(captured.ContentHash)}}",
             "Source":1,"Resolver":1,"Members":["dwelling.toml","goods.toml"]}
            """));

        Assert.Equal(RulesetDiagnosticCode.Bundle, Refusal(entries).Code);
    }

    private static List<KeyValuePair<string, byte[]>> Bundled() =>
        [.. RulesetBundle.Write(Capture(Manifest([.. Base.Select(member => member.Path)]), Base).Capture!)];

    private static JsonElement Envelope(IEnumerable<KeyValuePair<string, byte[]>> entries) =>
        JsonDocument.Parse(entries.First(entry => entry.Key == "bundle.json").Value).RootElement;

    private static RulesetDiagnostic Refusal(IEnumerable<KeyValuePair<string, byte[]>> entries)
    {
        RulesetCaptureResult read = RulesetBundle.Read(entries);

        Assert.False(read.Ok, "the bundle was read.");

        return read.Diagnostics[0];
    }
}
