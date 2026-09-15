using System.Text;
using Borough.Core.Rules;
using Borough.Core.Space;
using Tomlyn.Parsing;
using Tomlyn.Syntax;

namespace Borough.Formats;

/// <summary>One top-level declaration collected from a source package, in resolution order.</summary>
/// <param name="Section">The section: <c>rule</c>, <c>building</c>, <c>capacity</c>, ...</param>
/// <param name="Id">The typed id, or null for a singleton section.</param>
/// <param name="Label">The display label (the id when none is authored), or null for a singleton.</param>
/// <param name="Order">The explicit <c>order</c>; zero where absent or not accepted.</param>
/// <param name="Location">The declaration's header.</param>
public sealed record RulesetSourceDeclaration(
    string Section, string? Id, string? Label, long Order, RulesetSourceLocation Location);

/// <summary>
/// A resolved candidate: its capture, the Ruleset and display names, or located diagnostics.
/// </summary>
public sealed class RulesetSourceResult
{
    private readonly RulesetLoadResult? _loaded;

    internal RulesetSourceResult(
        RulesetCapture? capture,
        RulesetLoadResult? loaded,
        RulesetSourceDeclaration[] declarations,
        RulesetDiagnostic[] diagnostics)
    {
        Capture = capture;
        _loaded = loaded;
        Declarations = declarations;
        Diagnostics = diagnostics;
    }

    /// <summary>The captured bytes and identity, or null when capture itself was refused.</summary>
    public RulesetCapture? Capture { get; }

    /// <summary>The Ruleset, or null when the candidate was refused.</summary>
    public Ruleset? Ruleset => _loaded?.Ruleset;

    /// <summary>Display names for the Ruleset's ids: source labels for a package.</summary>
    public RulesetNames Names => _loaded?.Names ?? RulesetNames.None;

    /// <summary>A package's top-level declarations in resolution order. Empty for a single file.</summary>
    public IReadOnlyList<RulesetSourceDeclaration> Declarations { get; }

    /// <summary>Every reason the candidate was refused, sorted. Empty on success.</summary>
    public IReadOnlyList<RulesetDiagnostic> Diagnostics { get; }

    /// <summary>Whether there is a Ruleset to run.</summary>
    public bool Ok => Ruleset is not null;

    /// <summary>
    /// The candidate in the shape hosts consume today. A single-file entry returns the reader's own
    /// result unchanged; a package's refusals carry member paths and lines.
    /// </summary>
    public RulesetLoadResult ToLoadResult() =>
        _loaded ?? RulesetLoadResult.Refused(
            [.. Diagnostics.Select(d => new RulesetRefusal(d.Path, d.Line, d.Id, d.Reason))]);

    /// <summary>Every diagnostic, one per line.</summary>
    public string Describe() => string.Join(Environment.NewLine, Diagnostics);
}

/// <summary>
/// Resolves a captured Ruleset entry: the single-file reader unchanged, or a source v1 package.
/// </summary>
/// <remarks>
/// <para>
/// <b>Package resolution.</b> Every member is parsed and its top-level declarations are collected
/// with their locations before anything is resolved. A declaration is its header table plus any
/// nested tables that follow it in the same member. Array declarations require a typed
/// <c>id</c>; an optional <c>label</c> is display text only. Duplicate <c>(section, id)</c> pairs, a
/// singleton section in two places and a nested table away from its owner are refused naming both
/// locations where there are two. File and membership order grant no precedence.
/// </para>
/// <para>
/// <b>Ordering.</b> Sections are ordered by name; within one, declarations are ordered by ordinal
/// id, except <c>rule</c>, <c>policy</c> and <c>zone_rule</c>, ordered by <c>(order, id)</c>.
/// Dense runtime ids are therefore allocated from source ids, never from file placement.
/// </para>
/// <para>
/// <b>Lowering is development scaffolding.</b> The ordered declarations are re-emitted as one
/// document for the single-file reader, so every existing execution field keeps its validation:
/// <c>id</c> becomes the reader's identity <c>name</c> (identity keys therefore remain
/// <c>ContentHash.Of(UTF8(id))</c>), and <c>label</c>/<c>order</c> are blanked. Edits preserve line
/// structure, and the reader's refusals are mapped back to member lines; their column is unknown.
/// <c>terrain</c> keeps its closed-enum <c>name</c>; <c>hinterland</c> and <c>lattice</c> are
/// anonymous in the reader, so their id is not lowered. Shared baskets, recipes and storage
/// selections are refused until their runtime support exists; they are never approximated here.
/// </para>
/// </remarks>
public static class RulesetSource
{
    /// <summary>Captures and resolves the entry at <paramref name="entryPath"/>, for a new world.</summary>
    public static RulesetSourceResult Load(string entryPath) =>
        Resolve(RulesetCapture.Read(entryPath));

    /// <summary>
    /// Captures and resolves the entry at <paramref name="entryPath"/> for an existing world, refusing
    /// changes to its frozen Layer constants as <see cref="RulesetLoader.Reload"/> does.
    /// </summary>
    public static RulesetSourceResult Load(string entryPath, LayerConstants frozen) =>
        Resolve(RulesetCapture.Read(entryPath), frozen);

    /// <inheritdoc cref="Resolve(RulesetCapture)"/>
    public static RulesetSourceResult Resolve(RulesetCaptureResult captured)
    {
        ArgumentNullException.ThrowIfNull(captured);

        return captured.Capture is { } capture
            ? Resolve(capture)
            : new RulesetSourceResult(null, null, [], [.. captured.Diagnostics]);
    }

    /// <inheritdoc cref="Resolve(RulesetCapture, LayerConstants)"/>
    public static RulesetSourceResult Resolve(RulesetCaptureResult captured, LayerConstants frozen)
    {
        ArgumentNullException.ThrowIfNull(captured);

        return captured.Capture is { } capture
            ? Resolve(capture, frozen)
            : new RulesetSourceResult(null, null, [], [.. captured.Diagnostics]);
    }

    /// <summary>Resolves a capture for a world that does not exist yet.</summary>
    public static RulesetSourceResult Resolve(RulesetCapture capture) =>
        Resolve(capture, RulesetLoader.Parse);

    /// <summary>Resolves a capture for a world whose Layer constants are frozen.</summary>
    public static RulesetSourceResult Resolve(RulesetCapture capture, LayerConstants frozen) =>
        Resolve(capture, (text, fileName) => RulesetLoader.Reload(text, fileName, frozen));

    private static RulesetSourceResult Resolve(
        RulesetCapture capture, Func<string, string, RulesetLoadResult> read)
    {
        ArgumentNullException.ThrowIfNull(capture);

        if (capture.Mode == RulesetSourceMode.Legacy)
        {
            RulesetLoadResult legacy =
                read(RulesetCapture.DecodeAsSingleFile(capture.Entry), capture.EntryName);

            return new RulesetSourceResult(capture, legacy, [], RulesetDiagnostic.Sorted(
                legacy.Refusals.Select(r => new RulesetDiagnostic(
                    r.File, r.Line, 0, RulesetDiagnosticCode.Ruleset, null, r.Rule, r.Reason))));
        }

        var collection = new Collection(capture);
        collection.Collect();

        RulesetSourceDeclaration[] declarations = collection.Declarations();

        if (collection.Diagnostics.Count > 0)
        {
            return new RulesetSourceResult(
                capture, null, declarations, RulesetDiagnostic.Sorted(collection.Diagnostics));
        }

        Lowering lowering = collection.Lower();
        RulesetLoadResult lowered = read(lowering.Text, capture.EntryName);

        if (lowered.Ruleset is null)
        {
            return new RulesetSourceResult(capture, null, declarations,
                RulesetDiagnostic.Sorted(lowered.Refusals.Select(lowering.Map)));
        }

        return new RulesetSourceResult(
            capture,
            RulesetLoadResult.Accepted(lowered.Ruleset, lowered.Names.Relabelled(collection.LabelOf)),
            declarations,
            []);
    }

    /// <summary>The declarations of one package, collected before anything is resolved.</summary>
    private sealed class Collection(RulesetCapture capture)
    {
        private readonly List<Declared> _declared = [];
        private readonly Dictionary<(string Section, string Id), string> _labels = [];

        public List<RulesetDiagnostic> Diagnostics { get; } = [];

        public void Collect()
        {
            foreach (RulesetMember member in capture.Members)
            {
                if (!RulesetCapture.TryDecodeUtf8(member.Content.Span, out string text))
                {
                    Diagnostics.Add(new RulesetDiagnostic(member.Path, 0, 0, RulesetDiagnosticCode.Utf8,
                        null, null, "the member is not valid UTF-8."));
                    continue;
                }

                DocumentSyntax document = SyntaxParser.Parse(text, member.Path, validate: true);
                RulesetCapture.AddSyntax(Diagnostics, member.Path, document);

                if (!document.HasErrors)
                {
                    CollectMember(new Source(member.Path, text), document);
                }
            }

            RefuseDuplicates();

            foreach (Declared declared in _declared)
            {
                if (declared is { Valid: true, IsArray: true, Id: { } id })
                {
                    _labels.TryAdd((declared.Section, id), declared.Label ?? id);
                }
            }
        }

        public string LabelOf(string section, string id) =>
            _labels.TryGetValue((section, id), out string? label) ? label : id;

        public RulesetSourceDeclaration[] Declarations() =>
            [.. Ordered().Select(d => new RulesetSourceDeclaration(
                d.Section, d.Id, d.IsArray ? d.Label ?? d.Id : null, d.Order, d.Location))];

        public Lowering Lower()
        {
            // Line 1 belongs to no member, so the reader's whole-document refusals, which it reports
            // at line 1, map to the manifest instead of to whichever declaration sorts first.
            var text = new StringBuilder("# source v1 package lowered for the single-file reader\n");
            var segments = new List<Segment>();
            int line = 1;

            foreach (Declared declared in Ordered())
            {
                string chunk = declared.Render();
                int lines = chunk.Count(c => c == '\n');

                segments.Add(new Segment(line, lines, declared));
                text.Append(chunk);
                line += lines;
            }

            return new Lowering(text.ToString(), [.. segments], capture.EntryName);
        }

        private List<Declared> Ordered()
        {
            List<Declared> ordered = _declared.FindAll(d => d.Valid);

            ordered.Sort((a, b) =>
            {
                int order = string.CompareOrdinal(a.Section, b.Section);

                if (order == 0)
                {
                    order = a.IsArray.CompareTo(b.IsArray);
                }

                if (order == 0)
                {
                    order = a.Order.CompareTo(b.Order);
                }

                if (order == 0)
                {
                    order = string.CompareOrdinal(a.Id, b.Id);
                }

                return order == 0 ? CompareLocations(a.Location, b.Location) : order;
            });

            return ordered;
        }

        private void CollectMember(Source source, DocumentSyntax document)
        {
            foreach (KeyValueSyntax pair in document.KeyValues)
            {
                Refuse(RulesetSourceLocation.Of(source.Path, pair), RulesetDiagnosticCode.MemberShape,
                    null, null,
                    $"'{RulesetCapture.NameOf(pair.Key)}' is outside any table. A member contains "
                    + "only declarations.");
            }

            Declared? owner = null;

            foreach (TableSyntaxBase table in document.Tables)
            {
                string name = RulesetCapture.NameOf(table.Name);
                RulesetSourceLocation at = RulesetSourceLocation.Of(source.Path, table);
                int headerLine = table.Span.Start.Line;
                int dot = name.IndexOf('.', StringComparison.Ordinal);

                if (dot >= 0 && !RulesetCapture.IsSource(name))
                {
                    // A nested table belongs to the declaration above it, in this member only.
                    if (owner is not null && owner.Section == name[..dot])
                    {
                        continue;
                    }

                    Refuse(at, RulesetDiagnosticCode.MemberShape, null, null,
                        $"{Header(table, name)} does not follow a [[{name[..dot]}]] in this member. A "
                        + "nested table stays with its owning declaration, and a member cannot "
                        + "reopen one declared elsewhere.");
                }

                Close(owner, headerLine);
                owner = null;

                if (dot >= 0 && !RulesetCapture.IsSource(name))
                {
                    continue;
                }

                if (RulesetCapture.IsSource(name))
                {
                    Refuse(at, RulesetDiagnosticCode.MemberShape, null, null,
                        "a member cannot contain [source]. Only the entry manifest lists members.");
                    continue;
                }

                if (table is TableArraySyntax && name is "basket" or "recipe" or "storage")
                {
                    Refuse(at, RulesetDiagnosticCode.Unimplemented, null, null,
                        $"[[{name}]] is a source v1 shared definition this build does not implement "
                        + "yet. Shared baskets need saved fractional consumption progress in Core, "
                        + "and derived storage needs them; neither is approximated by the loader.");
                    continue;
                }

                owner = table is TableArraySyntax
                    ? CollectArray(source, table, name, at)
                    : new Declared(source, name, false, at, headerLine);

                _declared.Add(owner);
            }

            Close(owner, source.LineCount);
        }

        private Declared CollectArray(
            Source source, TableSyntaxBase table, string section, RulesetSourceLocation at)
        {
            var declared = new Declared(source, section, true, at, table.Span.Start.Line);
            KeyValueSyntax? id = null;
            KeyValueSyntax? label = null;
            KeyValueSyntax? order = null;
            KeyValueSyntax? name = null;

            foreach (KeyValueSyntax item in table.Items)
            {
                switch (RulesetCapture.NameOf(item.Key))
                {
                    case "id": id = item; break;
                    case "label": label = item; break;
                    case "order": order = item; break;
                    case "name": name = item; break;
                }
            }

            // terrain's name selects a closed enum member; hinterland and lattice have no identity key
            // in the single-file reader, so their id is collected but not lowered.
            bool lowersId = section is not ("terrain" or "hinterland" or "lattice");

            if (id is null)
            {
                declared.Valid = false;
                Refuse(at, RulesetDiagnosticCode.Id, null, null,
                    $"[[{section}]] has no id. Every source declaration is identified by id.");
            }
            else if (id.Value is not StringValueSyntax { Value: { } text } || !IsId(text))
            {
                declared.Valid = false;
                Refuse(RulesetSourceLocation.Of(source.Path, id), RulesetDiagnosticCode.Id, null, null,
                    $"[[{section}]] id must be a quoted string matching [A-Za-z0-9_][A-Za-z0-9_.-]*.");
            }
            else
            {
                declared.Id = text;
                declared.Edits.Add(lowersId ? Edit.Rename(id.Key!, "name") : Edit.Blank(id));
            }

            if (name is not null && lowersId)
            {
                declared.Valid = false;
                Refuse(RulesetSourceLocation.Of(source.Path, name), RulesetDiagnosticCode.Id, section,
                    declared.Id,
                    "name is not a source key here: id identifies the declaration and label is its "
                    + "display text.");
            }

            if (label is not null)
            {
                if (label.Value is StringValueSyntax { Value: { } words })
                {
                    declared.Label = words;
                    declared.Edits.Add(Edit.Blank(label));
                }
                else
                {
                    declared.Valid = false;
                    Refuse(RulesetSourceLocation.Of(source.Path, label), RulesetDiagnosticCode.Label,
                        section, declared.Id, "label must be a quoted string.");
                }
            }

            if (order is not null)
            {
                if (section is not ("rule" or "policy" or "zone_rule"))
                {
                    declared.Valid = false;
                    Refuse(RulesetSourceLocation.Of(source.Path, order), RulesetDiagnosticCode.Order,
                        section, declared.Id,
                        "order is accepted only on [[rule]], [[policy]] and [[zone_rule]]; other "
                        + "declarations are ordered by id.");
                }
                else if (order.Value is not IntegerValueSyntax { Value: >= 0 } number)
                {
                    declared.Valid = false;
                    Refuse(RulesetSourceLocation.Of(source.Path, order), RulesetDiagnosticCode.Order,
                        section, declared.Id, "order must be a nonnegative whole number.");
                }
                else
                {
                    declared.Order = number.Value;
                    declared.Edits.Add(Edit.Blank(order));
                }
            }

            foreach (KeyValueSyntax item in table.Items)
            {
                string key = RulesetCapture.NameOf(item.Key);

                if (section == "rule" && key is "basket" or "recipe")
                {
                    Refuse(RulesetSourceLocation.Of(source.Path, item),
                        RulesetDiagnosticCode.Unimplemented, section, declared.Id,
                        $"a Rule's {key} reference is not implemented by this build yet. State the "
                        + "Rule's inputs and outputs.");
                }
                else if (section == "building" && key == "bins" && item.Value is ArraySyntax bins)
                {
                    RefuseStorageSelections(source, bins, declared.Id);
                }
            }

            return declared;
        }

        private void RefuseStorageSelections(Source source, ArraySyntax bins, string? id)
        {
            foreach (ArrayItemSyntax bin in bins.Items)
            {
                if (bin.Value is not InlineTableSyntax fields)
                {
                    continue;
                }

                foreach (InlineTableItemSyntax field in fields.Items)
                {
                    if (field.KeyValue is { } pair && RulesetCapture.NameOf(pair.Key) == "storage")
                    {
                        Refuse(RulesetSourceLocation.Of(source.Path, pair),
                            RulesetDiagnosticCode.Unimplemented, "building", id,
                            "a Bin storage selection is not implemented by this build yet: it derives "
                            + "capacity from a shared basket and saved selections need runtime "
                            + "support. State the Bin's capacity.");
                    }
                }
            }
        }

        private void RefuseDuplicates()
        {
            List<Declared> valid = _declared.FindAll(d => d.Valid);

            valid.Sort((a, b) =>
            {
                int order = string.CompareOrdinal(a.Section, b.Section);

                if (order == 0)
                {
                    order = a.IsArray.CompareTo(b.IsArray);
                }

                if (order == 0)
                {
                    order = string.CompareOrdinal(a.Id, b.Id);
                }

                return order == 0 ? CompareLocations(a.Location, b.Location) : order;
            });

            int first = 0;

            for (int i = 1; i < valid.Count; i++)
            {
                Declared previous = valid[i - 1];
                Declared current = valid[i];

                if (previous.Section != current.Section)
                {
                    first = i;
                    continue;
                }

                if (previous.IsArray != current.IsArray)
                {
                    current.Valid = false;
                    Refuse(current.Location, RulesetDiagnosticCode.DeclarationKind, null, null,
                        $"[{current.Section}] and [[{current.Section}]] are both declared; the other "
                        + $"is at {previous.Location}.");
                    first = i;
                    continue;
                }

                if (current.IsArray && previous.Id != current.Id)
                {
                    first = i;
                    continue;
                }

                Declared origin = valid[first];
                current.Valid = false;

                if (current.IsArray)
                {
                    Refuse(current.Location, RulesetDiagnosticCode.DuplicateId, current.Section,
                        current.Id,
                        $"a second [[{current.Section}]] has id '{current.Id}'; the first is at "
                        + $"{origin.Location}. Duplicate ids are refused even when their values agree.");
                }
                else
                {
                    Refuse(current.Location, RulesetDiagnosticCode.SingletonOwner, current.Section, null,
                        $"[{current.Section}] is also declared at {origin.Location}. A singleton "
                        + "section has one owning member, and even disjoint fragments are refused.");
                }
            }
        }

        private void Refuse(
            RulesetSourceLocation at, string code, string? section, string? id, string reason) =>
            Diagnostics.Add(new RulesetDiagnostic(at.Path, at.Line, at.Column, code, section, id, reason));

        private static void Close(Declared? owner, int endLine)
        {
            if (owner is not null)
            {
                owner.EndLine = endLine;
            }
        }

        private static string Header(TableSyntaxBase table, string name) =>
            table is TableArraySyntax ? $"[[{name}]]" : $"[{name}]";

        private static bool IsId(string text)
        {
            if (text.Length == 0)
            {
                return false;
            }

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                bool word = c is (>= 'A' and <= 'Z') or (>= 'a' and <= 'z') or (>= '0' and <= '9') or '_';

                if (!word && !(i > 0 && c is '.' or '-'))
                {
                    return false;
                }
            }

            return true;
        }

        private static int CompareLocations(RulesetSourceLocation a, RulesetSourceLocation b)
        {
            int order = string.CompareOrdinal(a.Path, b.Path);

            if (order == 0)
            {
                order = a.Line.CompareTo(b.Line);
            }

            return order == 0 ? a.Column.CompareTo(b.Column) : order;
        }
    }

    /// <summary>A member's decoded text and the offset where each line starts.</summary>
    private sealed class Source
    {
        private readonly int[] _lineStarts;

        public Source(string path, string text)
        {
            Path = path;
            Text = text;

            List<int> starts = [0];

            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '\n')
                {
                    starts.Add(i + 1);
                }
            }

            _lineStarts = [.. starts];
        }

        public string Path { get; }

        public string Text { get; }

        public int LineCount => _lineStarts.Length;

        public int OffsetOfLine(int line) => line < _lineStarts.Length ? _lineStarts[line] : Text.Length;
    }

    /// <summary>One collected declaration: where it is, what it is called and how it lowers.</summary>
    private sealed class Declared(
        Source source, string section, bool isArray, RulesetSourceLocation location, int firstLine)
    {
        public Source Source { get; } = source;

        public string Section { get; } = section;

        public bool IsArray { get; } = isArray;

        public RulesetSourceLocation Location { get; } = location;

        /// <summary>Zero-based first line: the header.</summary>
        public int FirstLine { get; } = firstLine;

        /// <summary>Zero-based exclusive end line: the next top-level header, or the member's end.</summary>
        public int EndLine { get; set; } = firstLine + 1;

        public string? Id { get; set; }

        public string? Label { get; set; }

        public long Order { get; set; }

        public bool Valid { get; set; } = true;

        public List<Edit> Edits { get; } = [];

        /// <summary>The declaration's lines with its edits applied; line structure is unchanged.</summary>
        public string Render()
        {
            int start = Source.OffsetOfLine(FirstLine);
            int end = Source.OffsetOfLine(EndLine);
            var chunk = new StringBuilder(Source.Text, start, end - start, end - start + 8);

            foreach (Edit edit in Edits.OrderByDescending(e => e.Start))
            {
                int from = edit.Start - start;
                int length = edit.End - edit.Start + 1;

                if (edit.Replacement is null)
                {
                    for (int i = from; i < from + length; i++)
                    {
                        if (chunk[i] is not ('\n' or '\r'))
                        {
                            chunk[i] = ' ';
                        }
                    }
                }
                else
                {
                    chunk.Remove(from, length).Insert(from, edit.Replacement);
                }
            }

            if (chunk.Length == 0 || chunk[^1] != '\n')
            {
                chunk.Append('\n');
            }

            return chunk.ToString();
        }
    }

    /// <summary>A character range of a member (end inclusive) to rename, or to blank when null.</summary>
    private readonly record struct Edit(int Start, int End, string? Replacement)
    {
        public static Edit Rename(SyntaxNodeBase key, string replacement) =>
            new(key.Span.Start.Offset, key.Span.End.Offset, replacement);

        // From the key to the end of the value: a trailing comment and the line break survive.
        public static Edit Blank(KeyValueSyntax pair) =>
            new(pair.Key!.Span.Start.Offset, pair.Value!.Span.End.Offset, null);
    }

    private readonly record struct Segment(int Start, int Lines, Declared Declared);

    /// <summary>The lowered document, and how its lines map back to member lines.</summary>
    private sealed class Lowering(string text, Segment[] segments, string entryName)
    {
        public string Text { get; } = text;

        public RulesetDiagnostic Map(RulesetRefusal refusal)
        {
            int line = refusal.Line - 1;

            foreach (Segment segment in segments)
            {
                if (line >= segment.Start && line < segment.Start + segment.Lines)
                {
                    Declared declared = segment.Declared;
                    string reason = refusal.Rule is null || refusal.Rule == declared.Id
                        ? refusal.Reason
                        : $"'{refusal.Rule}': {refusal.Reason}";

                    return new RulesetDiagnostic(declared.Source.Path,
                        declared.FirstLine + (line - segment.Start) + 1, 0, RulesetDiagnosticCode.Ruleset,
                        declared.Section, declared.Id, reason);
                }
            }

            return new RulesetDiagnostic(entryName, 0, 0, RulesetDiagnosticCode.Ruleset, null,
                refusal.Rule, refusal.Reason);
        }
    }
}
