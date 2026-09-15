using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Borough.Formats;

/// <summary>
/// The stored shape of a captured Ruleset: what a save, a replay artifact or a content store holds,
/// and what a host reads one back from.
/// </summary>
/// <remarks>
/// <para>
/// A bundle is an entry-name/byte collection rather than a directory or an archive, so the host owns
/// the container and neither shell can extract an arbitrary archive path or develop its own
/// membership rules. <c>bundle.json</c> records the envelope version, the mode and the content
/// identity, and for a package the source and resolver versions and the sorted member paths. Source
/// mode stores the manifest as <c>source.toml</c> and member bytes beneath <c>members/</c>; legacy
/// mode stores <c>ruleset.toml</c> alone under its original hash.
/// </para>
/// <para>
/// The JSON spelling and the container's compression are not identity inputs. A read recaptures the
/// bytes through <see cref="RulesetCapture.FromEntries"/>, so membership, portable paths and UTF-8
/// are checked exactly as a directory capture checks them, and then refuses a recomputed identity
/// that differs from the recorded one. See <c>docs/ruleset-authoring.md</c>, "Bundle identity and
/// retention".
/// </para>
/// </remarks>
public static class RulesetBundle
{
    /// <summary>The bundle envelope this build writes and reads.</summary>
    public const uint EnvelopeVersion = 1;

    /// <summary>The entry holding the envelope.</summary>
    public const string MetadataEntry = "bundle.json";

    /// <summary>The entry holding a package's manifest.</summary>
    public const string ManifestEntry = "source.toml";

    /// <summary>The entry holding a single-file Ruleset.</summary>
    public const string LegacyEntry = "ruleset.toml";

    /// <summary>The prefix each package member's entry name carries.</summary>
    public const string MemberPrefix = "members/";

    private const string LegacyMode = "legacy";
    private const string SourceMode = "source";

    private static readonly JsonSerializerOptions Spelling = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
    };

    /// <summary>
    /// How a content identity is spelled wherever a host names one: sixteen lower-case hex digits.
    /// </summary>
    public static string Identity(ulong contentHash) =>
        contentHash.ToString("x16", CultureInfo.InvariantCulture);

    /// <summary>The entries holding <paramref name="capture"/>, in a deterministic order.</summary>
    public static IReadOnlyList<KeyValuePair<string, byte[]>> Write(RulesetCapture capture)
    {
        ArgumentNullException.ThrowIfNull(capture);

        bool source = capture.Mode == RulesetSourceMode.Source;

        var envelope = new Envelope(
            EnvelopeVersion,
            source ? SourceMode : LegacyMode,
            Identity(capture.ContentHash),
            source ? RulesetCapture.SourceVersion : null,
            source ? RulesetCapture.ResolverVersion : null,
            source ? [.. capture.Members.Select(member => member.Path)] : null);

        var entries = new List<KeyValuePair<string, byte[]>>(capture.Members.Count + 2)
        {
            new(MetadataEntry, JsonSerializer.SerializeToUtf8Bytes(envelope, Spelling)),
            new(source ? ManifestEntry : LegacyEntry, capture.Entry.ToArray()),
        };

        entries.AddRange(capture.Members.Select(member =>
            new KeyValuePair<string, byte[]>(MemberPrefix + member.Path, member.Content.ToArray())));

        return entries;
    }

    /// <summary>Recaptures the Ruleset held in <paramref name="entries"/>, or says why it cannot.</summary>
    public static RulesetCaptureResult Read(IEnumerable<KeyValuePair<string, byte[]>> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        var held = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        var diagnostics = new List<RulesetDiagnostic>();

        foreach (KeyValuePair<string, byte[]> entry in entries)
        {
            ArgumentNullException.ThrowIfNull(entry.Value);

            if (!held.TryAdd(entry.Key, entry.Value))
            {
                diagnostics.Add(Problem(entry.Key, "the bundle holds this entry twice."));
            }
        }

        if (diagnostics.Count > 0)
        {
            return RulesetCaptureResult.Refused(diagnostics);
        }

        if (!held.Remove(MetadataEntry, out byte[]? metadata))
        {
            return Refuse(Problem(MetadataEntry, $"the bundle has no {MetadataEntry}."));
        }

        Envelope? envelope;

        try
        {
            envelope = JsonSerializer.Deserialize<Envelope>(metadata, Spelling);
        }
        catch (JsonException)
        {
            return Refuse(Problem(MetadataEntry, "the bundle metadata is not readable JSON."));
        }

        if (envelope is null)
        {
            return Refuse(Problem(MetadataEntry, "the bundle metadata is empty."));
        }

        if (envelope.Version != EnvelopeVersion)
        {
            return Refuse(new RulesetDiagnostic(MetadataEntry, 0, 0,
                RulesetDiagnosticCode.BundleVersion, null, null,
                $"envelope version {envelope.Version} is not a version this build reads."));
        }

        if (!TryReadIdentity(envelope.Identity, out ulong recorded))
        {
            return Refuse(Problem(MetadataEntry,
                "the recorded identity is not sixteen lower-case hex digits."));
        }

        return envelope.Mode switch
        {
            LegacyMode => ReadLegacy(envelope, held, recorded),
            SourceMode => ReadSource(envelope, held, recorded),
            _ => Refuse(Problem(MetadataEntry, $"'{envelope.Mode}' is not a bundle mode.")),
        };
    }

    private static RulesetCaptureResult ReadLegacy(
        Envelope envelope, Dictionary<string, byte[]> held, ulong recorded)
    {
        if (envelope.Source is not null || envelope.Resolver is not null || envelope.Members is not null)
        {
            return Refuse(Problem(MetadataEntry,
                "legacy metadata records source versions or members, which it has none of."));
        }

        if (!held.Remove(LegacyEntry, out byte[]? content))
        {
            return Refuse(Problem(MetadataEntry, $"a legacy bundle has no {LegacyEntry}."));
        }

        return Undeclared(held)
            ?? Verify(RulesetCapture.FromEntries(LegacyEntry, content, []), recorded);
    }

    private static RulesetCaptureResult ReadSource(
        Envelope envelope, Dictionary<string, byte[]> held, ulong recorded)
    {
        if (envelope.Source != RulesetCapture.SourceVersion
            || envelope.Resolver != RulesetCapture.ResolverVersion)
        {
            return Refuse(new RulesetDiagnostic(MetadataEntry, 0, 0,
                RulesetDiagnosticCode.BundleVersion, null, null,
                $"source version {envelope.Source} and resolver version {envelope.Resolver} are not "
                + "the versions this build implements. Retained bytes are not an interpreter promise."));
        }

        if (envelope.Members is null)
        {
            return Refuse(Problem(MetadataEntry, "source metadata records no member list."));
        }

        if (!held.Remove(ManifestEntry, out byte[]? manifest))
        {
            return Refuse(Problem(MetadataEntry, $"a source bundle has no {ManifestEntry}."));
        }

        var members = new List<KeyValuePair<string, byte[]>>(held.Count);

        foreach (KeyValuePair<string, byte[]> entry in held)
        {
            if (entry.Key.StartsWith(MemberPrefix, StringComparison.Ordinal))
            {
                members.Add(new KeyValuePair<string, byte[]>(entry.Key[MemberPrefix.Length..], entry.Value));
            }
        }

        foreach (KeyValuePair<string, byte[]> member in members)
        {
            held.Remove(MemberPrefix + member.Key);
        }

        if (Undeclared(held) is { } undeclared)
        {
            return undeclared;
        }

        RulesetCaptureResult captured = RulesetCapture.FromEntries(ManifestEntry, manifest, members);

        if (captured.Capture is not { } capture)
        {
            return captured;
        }

        return capture.Members.Select(member => member.Path).SequenceEqual(envelope.Members, StringComparer.Ordinal)
            ? Verify(captured, recorded)
            : Refuse(Problem(MetadataEntry,
                "the recorded member list does not match the members the manifest lists."));
    }

    private static RulesetCaptureResult Verify(RulesetCaptureResult captured, ulong recorded) =>
        captured.Capture is { } capture && capture.ContentHash != recorded
            ? Refuse(new RulesetDiagnostic(MetadataEntry, 0, 0, RulesetDiagnosticCode.BundleIdentity,
                null, null,
                $"the bundle's content is {Identity(capture.ContentHash)}, not the recorded "
                + $"{Identity(recorded)}."))
            : captured;

    private static RulesetCaptureResult? Undeclared(Dictionary<string, byte[]> held)
    {
        if (held.Count == 0)
        {
            return null;
        }

        var diagnostics = new List<RulesetDiagnostic>(held.Count);

        foreach (string entry in held.Keys)
        {
            diagnostics.Add(Problem(entry, "the bundle holds an entry its metadata does not declare."));
        }

        return RulesetCaptureResult.Refused(diagnostics);
    }

    private static bool TryReadIdentity(string? text, out ulong identity)
    {
        identity = 0;

        return text?.Length == 16
            && ulong.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out identity)
            && string.Equals(Identity(identity), text, StringComparison.Ordinal);
    }

    private static RulesetDiagnostic Problem(string path, string reason) =>
        new(path, 0, 0, RulesetDiagnosticCode.Bundle, null, null, reason);

    private static RulesetCaptureResult Refuse(RulesetDiagnostic diagnostic) =>
        RulesetCaptureResult.Refused([diagnostic]);

    private sealed record Envelope(
        uint Version,
        string? Mode,
        string? Identity,
        uint? Source,
        uint? Resolver,
        string[]? Members);
}
