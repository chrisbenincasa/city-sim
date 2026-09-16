using System.Buffers.Binary;
using System.Globalization;
using System.Text;
using Tomlyn.Parsing;
using Tomlyn.Syntax;

namespace Borough.Formats;

/// <summary>Whether a captured entry is a single-file Ruleset or a source v1 manifest.</summary>
public enum RulesetSourceMode
{
    /// <summary>A document without <c>[source]</c>, read by the unchanged single-file reader.</summary>
    Legacy,

    /// <summary>A <c>[source] version = 1</c> manifest and the members it lists.</summary>
    Source,
}

/// <summary>A one-based position in a captured file. Zero means unknown.</summary>
public readonly record struct RulesetSourceLocation(string Path, int Line, int Column)
{
    /// <inheritdoc/>
    public override string ToString() =>
        string.Create(CultureInfo.InvariantCulture, $"{Path}:{Line}:{Column}");

    internal static RulesetSourceLocation Of(string path, SyntaxNodeBase node) =>
        new(path, node.Span.Start.Line + 1, node.Span.Start.Column + 1);
}

/// <summary>One listed member of a captured source package.</summary>
public sealed class RulesetMember
{
    private readonly byte[] _content;

    internal RulesetMember(string path, byte[] content)
    {
        Path = path;
        _content = content;
    }

    /// <summary>The portable path, relative to the manifest's directory.</summary>
    public string Path { get; }

    /// <summary>The bytes captured for this member, exactly as read.</summary>
    public ReadOnlyMemory<byte> Content => _content;
}

/// <summary>
/// An immutable capture of a Ruleset entry and, for a source package, every member it lists.
/// </summary>
/// <remarks>
/// <para>
/// Parsing, hashing, previews and retention all use one capture, so a disk edit after capture needs
/// another capture; it is not a filesystem transaction. The presence of <c>[source]</c> in the entry
/// selects the package reader. An entry without it is captured for the single-file reader with its
/// existing <see cref="RulesetFile.HashOfContent"/> identity. A malformed or unsupported
/// <c>[source]</c> is refused rather than read as a single-file Ruleset.
/// </para>
/// <para>
/// A package's <see cref="ContentHash"/> folds a framed bundle: <c>Borough.RulesetBundle\0</c>, then
/// source version, resolver version and member count as unsigned 32-bit little-endian integers, then
/// the manifest and each member in ordinal path order (path, then content) as byte strings prefixed
/// with an unsigned 64-bit little-endian length. Manifest and member content are CRLF-normalised
/// before framing; nothing else is. The entry's filename, absolute root and enumeration order do not
/// enter it. Manifest bytes do, so reordering <c>members</c> changes the identity without changing
/// resolution. See <c>docs/ruleset-authoring.md</c>, "Bundle identity and retention".
/// </para>
/// </remarks>
public sealed class RulesetCapture
{
    /// <summary>The <c>[source] version</c> this build reads, and the version framed into identity.</summary>
    public const uint SourceVersion = 1;

    /// <summary>The source interpretation this build implements, framed into identity.</summary>
    public const uint ResolverVersion = 1;

    /// <summary>The most members one manifest may list.</summary>
    public const int MemberLimit = 256;

    /// <summary>The most bytes one manifest or one member may hold.</summary>
    public const int ByteLimit = 4 << 20;

    private static readonly UTF8Encoding StrictUtf8 =
        new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    private readonly byte[] _entry;

    private RulesetCapture(
        RulesetSourceMode mode, string entryName, byte[] entry, RulesetMember[] members, ulong contentHash)
    {
        Mode = mode;
        EntryName = entryName;
        _entry = entry;
        Members = Array.AsReadOnly(members);
        ContentHash = contentHash;
    }

    /// <summary>Which reader this capture selects.</summary>
    public RulesetSourceMode Mode { get; }

    /// <summary>The entry's name as supplied, used in diagnostics. Not part of identity.</summary>
    public string EntryName { get; }

    /// <summary>The single-file Ruleset or the manifest, exactly as read.</summary>
    public ReadOnlyMemory<byte> Entry => _entry;

    /// <summary>A package's members in ordinal path order. Empty for a single-file Ruleset.</summary>
    public IReadOnlyList<RulesetMember> Members { get; }

    /// <summary>The content identity: the legacy hash, or a package's framed bundle hash.</summary>
    public ulong ContentHash { get; }

    /// <summary>Captures the entry at <paramref name="entryPath"/> and any members it lists.</summary>
    /// <remarks>
    /// The entry itself is read as <see cref="RulesetLoader.Load(string)"/> reads it, so a missing
    /// entry throws. Member problems are diagnostics located at the manifest line that lists them.
    /// Members are regular files beneath the manifest's directory, reached without symbolic links.
    /// </remarks>
    public static RulesetCaptureResult Read(string entryPath)
    {
        ArgumentException.ThrowIfNullOrEmpty(entryPath);

        byte[] entry = File.ReadAllBytes(entryPath);
        string root = Path.GetDirectoryName(Path.GetFullPath(entryPath)) ?? Path.GetFullPath(".");

        return Capture(
            entryPath,
            entry,
            relative => ReadMember(root, relative),
            supplied: [],
            self: Path.GetFileName(entryPath));
    }

    /// <summary>
    /// Captures an entry and its members from an entry-name/byte collection instead of a directory.
    /// </summary>
    /// <remarks>
    /// Membership must be exact: a listed member absent from <paramref name="members"/>, a supplied
    /// entry the manifest does not list, and a single-file entry supplied with members are refused.
    /// The enumeration order of <paramref name="members"/> is not significant.
    /// </remarks>
    public static RulesetCaptureResult FromEntries(
        string entryName, ReadOnlySpan<byte> entry, IEnumerable<KeyValuePair<string, byte[]>> members)
    {
        ArgumentException.ThrowIfNullOrEmpty(entryName);
        ArgumentNullException.ThrowIfNull(members);

        var supplied = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        var diagnostics = new List<RulesetDiagnostic>();

        foreach (KeyValuePair<string, byte[]> member in members)
        {
            ArgumentNullException.ThrowIfNull(member.Value);

            if (!supplied.TryAdd(member.Key, [.. member.Value]))
            {
                diagnostics.Add(new RulesetDiagnostic(member.Key, 0, 0,
                    RulesetDiagnosticCode.MemberDuplicate, null, null,
                    "the entry collection supplies this member twice."));
            }
        }

        if (diagnostics.Count > 0)
        {
            return RulesetCaptureResult.Refused(diagnostics);
        }

        return Capture(
            entryName,
            entry.ToArray(),
            path => supplied.TryGetValue(path, out byte[]? content)
                ? MemberRead.Found(content)
                : MemberRead.Missing(),
            supplied.Keys,
            self: null);
    }

    /// <summary>
    /// Decodes bytes exactly as <see cref="File.ReadAllText(string)"/> does, for the single-file reader.
    /// </summary>
    internal static string DecodeAsSingleFile(ReadOnlyMemory<byte> bytes)
    {
        using var stream = new MemoryStream(bytes.ToArray(), writable: false);
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);

        return reader.ReadToEnd();
    }

    /// <summary>Decodes strict UTF-8, accepting and removing one leading byte order mark.</summary>
    internal static bool TryDecodeUtf8(ReadOnlySpan<byte> bytes, out string text)
    {
        ReadOnlySpan<byte> preamble = Encoding.UTF8.Preamble;
        ReadOnlySpan<byte> body = bytes.StartsWith(preamble) ? bytes[preamble.Length..] : bytes;

        try
        {
            text = StrictUtf8.GetString(body);
            return true;
        }
        catch (DecoderFallbackException)
        {
            text = string.Empty;
            return false;
        }
    }

    internal static void AddSyntax(List<RulesetDiagnostic> into, string path, DocumentSyntax document)
    {
        foreach (DiagnosticMessage message in document.Diagnostics)
        {
            into.Add(new RulesetDiagnostic(path, message.Span.Start.Line + 1,
                message.Span.Start.Column + 1, RulesetDiagnosticCode.Syntax, null, null,
                message.Message));
        }
    }

    internal static string NameOf(KeySyntax? key) => key?.ToString().Trim() ?? string.Empty;

    internal static bool IsSource(string name) =>
        name == "source" || name.StartsWith("source.", StringComparison.Ordinal);

    /// <summary>
    /// Why <paramref name="path"/> is outside the portable member path vocabulary, or null.
    /// </summary>
    /// <remarks>
    /// Components are nonempty lower-case ASCII letters, digits, <c>_</c>, <c>-</c> and <c>.</c>,
    /// joined by <c>/</c>. The small vocabulary avoids case-folding and Unicode normalisation
    /// differences between filesystems, and keeps a package's meaning when the directory moves.
    /// </remarks>
    internal static string? PortablePathProblem(string path)
    {
        if (path.Length == 0)
        {
            return "is empty.";
        }

        if (path.Contains('\\', StringComparison.Ordinal))
        {
            return "contains a backslash. Member paths separate components with '/'.";
        }

        if (path[0] == '/')
        {
            return "is absolute. Member paths are relative to the manifest's directory.";
        }

        foreach (string component in path.Split('/'))
        {
            if (component.Length == 0)
            {
                return "has an empty component.";
            }

            if (component is "." or "..")
            {
                return $"has a '{component}' component. Member paths name each component "
                    + "beneath the manifest's directory.";
            }

            foreach (char c in component)
            {
                if (c is not ((>= 'a' and <= 'z') or (>= '0' and <= '9') or '_' or '-' or '.'))
                {
                    return $"contains '{c}'. Components use lower-case ASCII letters, digits, "
                        + "'_', '-' and '.'.";
                }
            }

            if (component[^1] == '.')
            {
                return $"has component '{component}' ending in a dot.";
            }

            int dot = component.IndexOf('.', StringComparison.Ordinal);

            if (IsReservedDeviceName(dot < 0 ? component : component[..dot]))
            {
                return $"has component '{component}', which is a reserved Windows device name.";
            }
        }

        return path.EndsWith(".toml", StringComparison.Ordinal) ? null : "does not end in .toml.";
    }

    private static bool IsReservedDeviceName(string basename) =>
        basename is "con" or "prn" or "aux" or "nul"
        || (basename.Length == 4
            && (basename.StartsWith("com", StringComparison.Ordinal)
                || basename.StartsWith("lpt", StringComparison.Ordinal))
            && basename[3] is >= '0' and <= '9');

    private static RulesetCaptureResult Capture(
        string entryName,
        byte[] entry,
        Func<string, MemberRead> read,
        IReadOnlyCollection<string> supplied,
        string? self)
    {
        DocumentSyntax probe = SyntaxParser.Parse(DecodeAsSingleFile(entry), entryName, validate: true);
        var diagnostics = new List<RulesetDiagnostic>();

        if (!DeclaresSource(probe))
        {
            foreach (string path in supplied)
            {
                diagnostics.Add(new RulesetDiagnostic(path, 0, 0, RulesetDiagnosticCode.MemberUnlisted,
                    null, null, "is supplied with a single-file Ruleset, which has no members."));
            }

            return diagnostics.Count > 0
                ? RulesetCaptureResult.Refused(diagnostics)
                : RulesetCaptureResult.Accepted(new RulesetCapture(RulesetSourceMode.Legacy, entryName,
                    entry, [], RulesetFile.HashOfContent(entry)));
        }

        if (entry.Length > ByteLimit)
        {
            diagnostics.Add(new RulesetDiagnostic(entryName, 0, 0, RulesetDiagnosticCode.Limit, null,
                null, $"the manifest holds {entry.Length} bytes; the limit is {ByteLimit}."));

            return RulesetCaptureResult.Refused(diagnostics);
        }

        if (!TryDecodeUtf8(entry, out string text))
        {
            diagnostics.Add(new RulesetDiagnostic(entryName, 0, 0, RulesetDiagnosticCode.Utf8, null,
                null, "the manifest is not valid UTF-8."));

            return RulesetCaptureResult.Refused(diagnostics);
        }

        DocumentSyntax manifest = SyntaxParser.Parse(text, entryName, validate: true);
        AddSyntax(diagnostics, entryName, manifest);

        if (manifest.HasErrors)
        {
            return RulesetCaptureResult.Refused(diagnostics);
        }

        List<Listed> listed = ReadManifest(manifest, entryName, self, diagnostics);
        var members = new List<RulesetMember>(listed.Count);

        foreach (Listed item in listed)
        {
            MemberRead found = read(item.Path);

            if (found.Content is null)
            {
                diagnostics.Add(new RulesetDiagnostic(entryName, item.At.Line, item.At.Column,
                    found.Code, null, null, $"member '{item.Path}' {found.Problem}"));
            }
            else if (!TryDecodeUtf8(found.Content, out _))
            {
                diagnostics.Add(new RulesetDiagnostic(item.Path, 0, 0, RulesetDiagnosticCode.Utf8,
                    null, null, "the member is not valid UTF-8."));
            }
            else
            {
                members.Add(new RulesetMember(item.Path, found.Content));
            }
        }

        if (diagnostics.Count == 0)
        {
            var paths = new HashSet<string>(StringComparer.Ordinal);

            foreach (Listed item in listed)
            {
                paths.Add(item.Path);
            }

            foreach (string path in supplied)
            {
                if (!paths.Contains(path))
                {
                    diagnostics.Add(new RulesetDiagnostic(path, 0, 0,
                        RulesetDiagnosticCode.MemberUnlisted, null, null,
                        "is supplied but the manifest does not list it. Only listed members participate."));
                }
            }
        }

        if (diagnostics.Count > 0)
        {
            return RulesetCaptureResult.Refused(diagnostics);
        }

        members.Sort((a, b) => string.CompareOrdinal(a.Path, b.Path));
        RulesetMember[] sorted = [.. members];

        return RulesetCaptureResult.Accepted(new RulesetCapture(
            RulesetSourceMode.Source, entryName, entry, sorted, FramedHash(entry, sorted)));
    }

    private static bool DeclaresSource(DocumentSyntax document)
    {
        foreach (TableSyntaxBase table in document.Tables)
        {
            if (IsSource(NameOf(table.Name)))
            {
                return true;
            }
        }

        foreach (KeyValueSyntax pair in document.KeyValues)
        {
            if (IsSource(NameOf(pair.Key)))
            {
                return true;
            }
        }

        return false;
    }

    private static List<Listed> ReadManifest(
        DocumentSyntax manifest, string entryName, string? self, List<RulesetDiagnostic> diagnostics)
    {
        List<Listed> listed = [];

        void Refuse(SyntaxNodeBase node, string code, string reason)
        {
            RulesetSourceLocation at = RulesetSourceLocation.Of(entryName, node);
            diagnostics.Add(new RulesetDiagnostic(entryName, at.Line, at.Column, code, null, null, reason));
        }

        foreach (KeyValueSyntax pair in manifest.KeyValues)
        {
            Refuse(pair, RulesetDiagnosticCode.Manifest,
                $"'{NameOf(pair.Key)}' is outside [source]. A manifest contains only [source]; "
                + "declarations belong in members.");
        }

        TableSyntaxBase? source = null;

        foreach (TableSyntaxBase table in manifest.Tables)
        {
            string name = NameOf(table.Name);

            if (name == "source" && table is TableSyntax)
            {
                source = table;
                continue;
            }

            Refuse(table, RulesetDiagnosticCode.Manifest, name == "source"
                ? "[[source]] is an array of tables. A manifest declares one [source] table."
                : $"[{name}] is in the manifest. A manifest contains only [source]; declarations "
                    + "belong in members.");
        }

        if (source is null)
        {
            return listed;
        }

        KeyValueSyntax? version = null;
        KeyValueSyntax? members = null;

        foreach (KeyValueSyntax item in source.Items)
        {
            switch (NameOf(item.Key))
            {
                case "version":
                    version = item;
                    break;

                case "members":
                    members = item;
                    break;

                default:
                    Refuse(item, RulesetDiagnosticCode.Manifest,
                        $"[source] has no key '{NameOf(item.Key)}'. It accepts exactly version and members.");
                    break;
            }
        }

        if (version is null)
        {
            Refuse(source, RulesetDiagnosticCode.Manifest, "[source] has no version. Write version = 1.");
        }
        else if (version.Value is not IntegerValueSyntax number)
        {
            Refuse(version, RulesetDiagnosticCode.Manifest, "version must be a whole number.");
        }
        else if (number.Value != SourceVersion)
        {
            Refuse(version, RulesetDiagnosticCode.Version,
                $"source version {number.Value} is not supported. This build reads version {SourceVersion}.");
        }

        if (members is null)
        {
            Refuse(source, RulesetDiagnosticCode.Manifest,
                "[source] has no members. List every member file explicitly.");

            return listed;
        }

        if (members.Value is not ArraySyntax array)
        {
            Refuse(members, RulesetDiagnosticCode.Manifest,
                "members must be an array of quoted relative paths.");

            return listed;
        }

        int count = array.Items.ChildrenCount;

        if (count > MemberLimit)
        {
            Refuse(members, RulesetDiagnosticCode.Limit,
                $"[source] lists {count} members; the limit is {MemberLimit}.");

            return listed;
        }

        foreach (ArrayItemSyntax item in array.Items)
        {
            if (item.Value is not StringValueSyntax { Value: { } path } text)
            {
                Refuse((SyntaxNodeBase?)item.Value ?? item, RulesetDiagnosticCode.Manifest,
                    "each member must be a quoted relative path.");
                continue;
            }

            string? problem = PortablePathProblem(path)
                ?? (path == self ? "is the manifest itself. A manifest cannot list itself." : null);

            if (problem is not null)
            {
                Refuse(text, RulesetDiagnosticCode.MemberPath, $"member '{path}' {problem}");
                continue;
            }

            int first = listed.FindIndex(entry => entry.Path == path);

            if (first >= 0)
            {
                Refuse(text, RulesetDiagnosticCode.MemberDuplicate,
                    $"member '{path}' is listed twice; the first is at {listed[first].At}.");
                continue;
            }

            listed.Add(new Listed(path, RulesetSourceLocation.Of(entryName, text)));
        }

        return listed;
    }

    private static MemberRead ReadMember(string root, string relative)
    {
        string current = root;
        string[] components = relative.Split('/');

        for (int i = 0; i < components.Length; i++)
        {
            current = Path.Combine(current, components[i]);
            bool last = i == components.Length - 1;
            FileSystemInfo info = last ? new FileInfo(current) : new DirectoryInfo(current);

            // Checked before existence: a dangling link is still a link.
            if (info.LinkTarget is not null)
            {
                return MemberRead.Refused(
                    "passes through a symbolic link. Members are regular files beneath the manifest's "
                    + "directory.");
            }

            if (!last && !info.Exists)
            {
                return MemberRead.Missing();
            }
        }

        if (Directory.Exists(current))
        {
            return MemberRead.Refused("is a directory, not a regular file.");
        }

        if (!File.Exists(current))
        {
            return MemberRead.Missing();
        }

        try
        {
            using FileStream stream = File.OpenRead(current);

            if (stream.Length > ByteLimit)
            {
                return MemberRead.TooLarge(stream.Length);
            }

            byte[] content = new byte[stream.Length];
            stream.ReadExactly(content);

            return MemberRead.Found(content);
        }
        catch (IOException exception)
        {
            return MemberRead.Refused($"could not be read: {exception.Message}");
        }
        catch (UnauthorizedAccessException exception)
        {
            return MemberRead.Refused($"could not be read: {exception.Message}");
        }
    }

    private static ulong FramedHash(ReadOnlySpan<byte> manifest, RulesetMember[] members)
    {
        using var frame = new MemoryStream();

        frame.Write("Borough.RulesetBundle\0"u8);
        WriteUInt32(frame, SourceVersion);
        WriteUInt32(frame, ResolverVersion);
        WriteUInt32(frame, checked((uint)members.Length));
        WriteBytes(frame, RulesetFile.WithoutCarriageReturnPairs(manifest));

        foreach (RulesetMember member in members)
        {
            WriteBytes(frame, Encoding.UTF8.GetBytes(member.Path));
            WriteBytes(frame, RulesetFile.WithoutCarriageReturnPairs(member.Content.Span));
        }

        return Borough.Core.Determinism.ContentHash.Of(
            frame.GetBuffer().AsSpan(0, checked((int)frame.Length)));
    }

    private static void WriteUInt32(MemoryStream into, uint value)
    {
        Span<byte> bytes = stackalloc byte[sizeof(uint)];
        BinaryPrimitives.WriteUInt32LittleEndian(bytes, value);
        into.Write(bytes);
    }

    private static void WriteBytes(MemoryStream into, ReadOnlySpan<byte> value)
    {
        Span<byte> length = stackalloc byte[sizeof(ulong)];
        BinaryPrimitives.WriteUInt64LittleEndian(length, (ulong)value.Length);
        into.Write(length);
        into.Write(value);
    }

    private readonly record struct Listed(string Path, RulesetSourceLocation At);

    private readonly record struct MemberRead(byte[]? Content, string Code, string Problem)
    {
        public static MemberRead Found(byte[] content) =>
            content.Length > ByteLimit
                ? TooLarge(content.Length)
                : new(content, string.Empty, string.Empty);

        public static MemberRead TooLarge(long bytes) =>
            new(null, RulesetDiagnosticCode.Limit,
                $"holds {bytes} bytes; the limit is {ByteLimit}.");

        public static MemberRead Missing() =>
            new(null, RulesetDiagnosticCode.MemberMissing,
                "is missing. A listed member never falls back to another file.");

        public static MemberRead Refused(string problem) =>
            new(null, RulesetDiagnosticCode.MemberRead, problem);
    }
}

/// <summary>A capture, or the diagnostics explaining why there is none. Never both.</summary>
public sealed class RulesetCaptureResult
{
    private RulesetCaptureResult(RulesetCapture? capture, RulesetDiagnostic[] diagnostics)
    {
        Capture = capture;
        Diagnostics = diagnostics;
    }

    /// <summary>The capture, or null when it was refused.</summary>
    public RulesetCapture? Capture { get; }

    /// <summary>Every reason capture was refused, sorted. Empty on success.</summary>
    public IReadOnlyList<RulesetDiagnostic> Diagnostics { get; }

    /// <summary>Whether there is a capture to resolve.</summary>
    public bool Ok => Capture is not null;

    internal static RulesetCaptureResult Accepted(RulesetCapture capture) => new(capture, []);

    internal static RulesetCaptureResult Refused(List<RulesetDiagnostic> diagnostics) =>
        new(null, RulesetDiagnostic.Sorted(diagnostics));
}
