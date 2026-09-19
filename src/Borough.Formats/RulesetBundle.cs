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
/// membership rules. <c>bundle.json</c> records the envelope version, the mode, the content identity
/// and the file name the entry was authored under, and for a package the source and resolver
/// versions and the sorted member paths. Source mode stores the manifest as <c>source.toml</c> and
/// member bytes beneath <c>members/</c>; legacy mode stores <c>ruleset.toml</c> alone under its
/// original hash.
/// </para>
/// <para>
/// The recorded name is what a host displays for a Ruleset it no longer has a path to, and a read
/// restores it as the capture's <see cref="RulesetCapture.EntryName"/>. It is a bare file name, never
/// a path, and carries no directory from the machine that wrote it.
/// </para>
/// <para>
/// The JSON spelling, the recorded name and the container's compression are not identity inputs. A
/// read recaptures the
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

    private const int NameLimit = 255;

    /// <summary>The envelope, the manifest and a full complement of members.</summary>
    private const int EntryLimit = RulesetCapture.MemberLimit + 2;

    private static readonly char[] Separators = ['/', '\\'];

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
            source ? [.. capture.Members.Select(member => member.Path)] : null,
            Named(capture.EntryName));

        var entries = new List<KeyValuePair<string, byte[]>>(capture.Members.Count + 2)
        {
            new(MetadataEntry, JsonSerializer.SerializeToUtf8Bytes(envelope, Spelling)),
            new(source ? ManifestEntry : LegacyEntry, capture.Entry.ToArray()),
        };

        entries.AddRange(capture.Members.Select(member =>
            new KeyValuePair<string, byte[]>(MemberPrefix + member.Path, member.Content.ToArray())));

        return entries;
    }

    /// <summary>
    /// The file name an entry was authored under, without the directories it was read from.
    /// </summary>
    /// <remarks>
    /// A capture's <see cref="RulesetCapture.EntryName"/> is the path its host supplied, which for the
    /// Godot shell is absolute. Only the last component is recorded, so a stored bundle names the
    /// author's file without carrying the author's directory tree into a shared save.
    /// </remarks>
    private static string Named(string entryName)
    {
        int cut = entryName.LastIndexOfAny(Separators);
        string name = cut < 0 ? entryName : entryName[(cut + 1)..];

        // What Write records, Read has to accept. A host may supply any entry name, and a bundle
        // that refuses its own name on the way back in would fail at the resume rather than here.
        return NameProblem(name) is null ? name : throw new ArgumentException(
            $"'{entryName}' has no file name a bundle can record: {NameProblem(name)}",
            nameof(entryName));
    }

    /// <summary>Recaptures the Ruleset held in <paramref name="entries"/>, or says why it cannot.</summary>
    public static RulesetCaptureResult Read(IEnumerable<KeyValuePair<string, byte[]>> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        var held = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        var diagnostics = new List<RulesetDiagnostic>();

        foreach (KeyValuePair<string, byte[]> entry in entries)
        {
            if (entry.Key is null || entry.Value is null)
            {
                diagnostics.Add(Problem(MetadataEntry,
                    "the bundle holds an entry with no name or no content."));
                continue;
            }

            if (held.Count == EntryLimit)
            {
                diagnostics.Add(new RulesetDiagnostic(MetadataEntry, 0, 0,
                    RulesetDiagnosticCode.Limit, null, null,
                    $"the bundle holds more than {EntryLimit} entries, which is a manifest, its "
                    + $"members and the envelope at the {RulesetCapture.MemberLimit}-member limit."));
                break;
            }

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

        if (metadata.Length > RulesetCapture.ByteLimit)
        {
            return Refuse(new RulesetDiagnostic(MetadataEntry, 0, 0, RulesetDiagnosticCode.Limit,
                null, null,
                $"the envelope holds {metadata.Length} bytes; the limit is "
                + $"{RulesetCapture.ByteLimit}."));
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

        if (NameProblem(envelope.Name) is { } problem)
        {
            return Refuse(Problem(MetadataEntry, problem));
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
            ?? Verify(RulesetCapture.FromEntries(envelope.Name ?? LegacyEntry, content, []), recorded);
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

        RulesetCaptureResult captured =
            RulesetCapture.FromEntries(envelope.Name ?? ManifestEntry, manifest, members);

        if (captured.Capture is not { } capture)
        {
            return captured;
        }

        // Sorted rather than as written: the manifest lists members in the author's order and the
        // capture sorts them, so a list that names the same members is the same list.
        string[] listed = [.. envelope.Members.Order(StringComparer.Ordinal)];

        return capture.Members.Select(member => member.Path).SequenceEqual(listed, StringComparer.Ordinal)
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

    /// <summary>Why the recorded name cannot be an entry name, or null.</summary>
    /// <remarks>
    /// The name reaches diagnostics as a path, so it stays a bare file name. A bundle written before
    /// the field existed records none, and reads under the codec's own entry name.
    /// </remarks>
    private static string? NameProblem(string? name)
    {
        if (name is null)
        {
            return null;
        }

        if (name.Length == 0 || name.Length > NameLimit)
        {
            return $"the recorded name is empty or longer than {NameLimit} characters.";
        }

        if (name.IndexOfAny(Separators) >= 0 || name is "." or "..")
        {
            return "the recorded name is a path rather than a file name.";
        }

        return null;
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
        string[]? Members,
        string? Name);
}
