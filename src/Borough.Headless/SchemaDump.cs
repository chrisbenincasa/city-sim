using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Borough.Formats;

namespace Borough.Headless;

/// <summary>
/// The Ruleset key surface, written out as a JSON Schema an editor can complete against.
/// </summary>
/// <remarks>
/// <para>
/// <b>Generated, because the loader already owns this and a hand-written schema would be a second
/// copy of it.</b> <c>RulesetLoader.Find</c> records every key any reader asks for, and
/// <c>RefuseUnknownKeys</c> refuses anything outside that set — so the permitted surface is
/// <em>derived from the code that does the reading</em>. Authoring a schema beside it would be
/// <c>plans/0012</c> <b>Cause 1</b> by construction: ***every document that stores what another
/// document owns drifted.*** This asks the loader instead.
/// </para>
/// <para>
/// ⚠ <b>It unions across every Ruleset in the folder and it still cannot be complete.</b> A reader
/// only asks a table that exists, so a section no shipped file declares contributes nothing at all.
/// That is why the schema leaves <c>additionalProperties</c> open — and it costs nothing, because
/// the loader refuses an unknown key at the parse site with a spelling suggestion and the full key
/// list, which is a better message than any schema produces. ***The schema is autocomplete; the
/// loader is the authority.***
/// </para>
/// <para>
/// 🔴 ⚠ <b>A LISTED KEY IS NOT NECESSARILY A LOADABLE ONE, and this is not fixable from here.</b>
/// Nine keys are known to the loader only so that it can refuse them by name with a sentence
/// saying where they went — <c>condemn_after</c>, <c>[[business]] wage</c>, <c>storage</c> — and a
/// named refusal reaches <c>Find</c> exactly as a real read does. ***The two are indistinguishable
/// in the surface***: the same presence test that spots <c>condemn_after</c> also spots
/// <c>capacity_per_cell</c> and <c>sealing_decay_tau</c>, which are real. Separating them would
/// mean hand-labelling the refusal sites, which is the second copy this whole approach exists to
/// avoid — so the schema completes them and the loader corrects you.
/// </para>
/// <para>
/// ✅ <b>Every key now carries a description, and the gap this paragraph used to record is
/// closed.</b> It said: <em>"No key carries a description … what a key means lives in the Ruleset
/// headers and the loader's doc comments, and neither is attributable to a key mechanically. A
/// description would have to be authored — at which point it is a second copy again."</em> Both
/// halves were right. <see cref="RulesetKeyNotes"/> authors the sentence, and
/// <c>RulesetKeyNoteTests</c> is what stops it being a copy: it fails when a key has no note
/// <em>and</em> when a note has no key, so neither side can move without the other. ***An authored
/// list a test holds against the code is a different object from an authored list nothing
/// compares***, and only the second is <c>plans/0012</c> Cause 1.
/// </para>
/// <para>
/// ⚠ <b>A key with no note still emits, silently.</b> The note is looked up and omitted where it is
/// absent rather than refused here — the coverage test is the instrument for that, and a schema dump
/// that threw would turn one missing sentence into no autocomplete at all.
/// </para>
/// </remarks>
internal static class SchemaDump
{
    /// <summary>Where the schema says it comes from. Not fetched — Taplo resolves the local path.</summary>
    private const string Id = "https://github.com/chrisbenincasa/city-sim/rulesets/ruleset.schema.json";

    public static int Print(Options options)
    {
        string folder = Path.GetDirectoryName(Path.GetFullPath(options.RulesetPaths[0])) ?? ".";
        string[] files = Directory.GetFiles(folder, "*.toml");

        Array.Sort(files, StringComparer.Ordinal);

        if (files.Length == 0)
        {
            Console.Error.WriteLine(
                $"no .toml files in {folder}. The schema is unioned across every Ruleset in the "
                + "folder holding --ruleset, because one file only ever declares the sections it "
                + "happens to demonstrate.");

            return 1;
        }

        var root = new Section();
        int refused = 0;

        foreach (string file in files)
        {
            string text = File.ReadAllText(file);

            // A refused file still contributes: every reader has run and recorded what it asked for
            // by the time Read returns, whatever it concluded about the file.
            if (!RulesetLoader.Parse(text, Path.GetFileName(file)).Ok)
            {
                refused++;
            }

            Absorb(root, RulesetLoader.KeySurface(text, Path.GetFileName(file)));
        }

        Console.Error.WriteLine(
            $"{files.Length} Ruleset(s) read from {folder}"
            + (refused > 0 ? $", {refused} of which the loader refuses" : string.Empty)
            + $"; {root.Children.Count} section(s), {Keys(root)} key(s).");

        Console.Out.Write(Render(root));
        Console.Out.Flush();

        return 0;
    }

    /// <summary>One section, or one key that turned out to hold a table.</summary>
    private sealed class Section
    {
        /// <summary>Written <c>[[x]]</c> rather than <c>[x]</c>, or holding an array of tables.</summary>
        public bool Repeats { get; set; }

        /// <summary>The scalar shape, where this is a leaf. <c>Unknown</c> emits no type at all.</summary>
        public RulesetKeyKind Kind { get; set; }

        /// <summary>
        /// What the key does, from <see cref="RulesetKeyNotes"/>, or <c>null</c> where none is
        /// authored.
        /// </summary>
        /// <remarks>
        /// <b>Set once and never overwritten with <c>null</c></b>, on <see cref="Kind"/>'s
        /// reasoning: one section appears once per table of its shape, and a node reached first
        /// through a path traversal has no note of its own to offer.
        /// </remarks>
        public string? Note { get; set; }

        public SortedDictionary<string, Section> Children { get; } = new(StringComparer.Ordinal);
    }

    private static int Keys(Section section)
    {
        int count = 0;

        foreach (KeyValuePair<string, Section> child in section.Children)
        {
            count += 1 + Keys(child.Value);
        }

        return count;
    }

    /// <summary>
    /// Folds one file's surface into the tree, splitting the loader's own context strings.
    /// </summary>
    /// <remarks>
    /// <b>The context is written the way the file writes it</b> — <c>RulesetLoader.ContextOf</c>
    /// produces <c>[layers]</c>, <c>[[building]]</c> and, for an inline table, the enclosing path
    /// with the key appended: <c>[[rule]] inputs</c>. So the first token carries the repeat marker
    /// and every later one is a plain key.
    /// </remarks>
    private static void Absorb(
        Section root, IReadOnlyDictionary<string, IReadOnlyDictionary<string, RulesetKeyKind>> surface)
    {
        foreach (KeyValuePair<string, IReadOnlyDictionary<string, RulesetKeyKind>> entry in surface)
        {
            string[] path = entry.Key.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (path.Length == 0 || !path[0].StartsWith('['))
            {
                // A key above the first section header. The loader refuses it by name; there is no
                // place in a schema to put a key that belongs to no table.
                continue;
            }

            Section at = root;

            for (int step = 0; step < path.Length; step++)
            {
                bool repeats = step == 0 && path[step].StartsWith("[[", StringComparison.Ordinal);
                string name = path[step].Trim('[', ']');

                if (!at.Children.TryGetValue(name, out Section? child))
                {
                    child = new Section();
                    at.Children[name] = child;
                }

                child.Repeats |= repeats;
                at = child;
            }

            foreach (KeyValuePair<string, RulesetKeyKind> key in entry.Value)
            {
                if (!at.Children.TryGetValue(key.Key, out Section? leaf))
                {
                    leaf = new Section();
                    at.Children[key.Key] = leaf;
                }

                // A nested context may have created this node already, and a kind recorded by a
                // reader beats the absence of one -- but never overwrite a known kind with Unknown.
                if (leaf.Kind == RulesetKeyKind.Unknown)
                {
                    leaf.Kind = key.Value;
                }

                leaf.Note ??= RulesetKeyNotes.For(entry.Key, key.Key);

                leaf.Repeats |= key.Value == RulesetKeyKind.Array;
            }
        }
    }

    private static string Render(Section root)
    {
        var buffer = new MemoryStream();

        using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions
            {
                Indented = true,
                // Otherwise a backtick in the description comes out as \u0060 and the file reads
                // like an escape sequence dump. The output is a local artefact, never a response.
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            }))
        {
            writer.WriteStartObject();
            writer.WriteString("$schema", "https://json-schema.org/draft/2020-12/schema");
            writer.WriteString("$id", Id);
            writer.WriteString("title", "Borough Ruleset");
            writer.WriteString(
                "description",
                "GENERATED from RulesetLoader by `--schema` -- do not edit; RulesetSchemaTests "
                + "fails when this file drifts from the loader. The loader is the authority and "
                + "this is autocomplete: unknown keys stay permitted here because the loader "
                + "refuses them at the parse site with a spelling suggestion. A key listed with no "
                + "`type` was asked for by a reader that never asserted one. Keys the loader knows "
                + "ONLY in order to refuse them by name are not here at all: they stay permitted "
                + "so that writing one gets the sentence saying where the key went, and they are "
                + "not offered, because a completion that always refuses the file is worse than "
                + "no completion.");
            writer.WriteString("type", "object");
            WriteProperties(writer, root);
            writer.WriteEndObject();
        }

        // A trailing newline, because every other text artefact in this repository has one and a
        // file that does not is a diff that never settles.
        return Encoding.UTF8.GetString(buffer.ToArray()) + "\n";
    }

    private static void WriteProperties(Utf8JsonWriter writer, Section section)
    {
        if (section.Children.Count == 0)
        {
            return;
        }

        writer.WriteStartObject("properties");

        foreach (KeyValuePair<string, Section> child in section.Children)
        {
            writer.WriteStartObject(child.Key);
            WriteBody(writer, child.Value);
            writer.WriteEndObject();
        }

        writer.WriteEndObject();
    }

    private static void WriteBody(Utf8JsonWriter writer, Section section)
    {
        // Before the type, because an editor shows the first line of a hover and a schema is read by
        // a person more often than by a validator.
        if (section.Note is not null)
        {
            writer.WriteString("description", section.Note);
        }

        bool holdsTable = section.Children.Count > 0
            || section.Kind is RulesetKeyKind.Table or RulesetKeyKind.Array;

        if (!holdsTable)
        {
            switch (section.Kind)
            {
                case RulesetKeyKind.Whole:
                    writer.WriteString("type", "integer");
                    break;

                case RulesetKeyKind.Quoted:
                    writer.WriteString("type", "string");
                    break;

                case RulesetKeyKind.Numbers:
                    // A bare list of whole numbers, which no other key in this Ruleset is. It heads
                    // no section, so the array is written here rather than through the Repeats path
                    // that array-of-tables keys take.
                    writer.WriteString("type", "array");
                    writer.WriteStartObject("items");
                    writer.WriteString("type", "integer");
                    writer.WriteEndObject();
                    break;

                default:
                    // Asked for by a reader that wanted only a line number, so nothing is known
                    // about the shape. An empty schema completes the key and validates anything.
                    break;
            }

            return;
        }

        if (section.Repeats)
        {
            writer.WriteString("type", "array");
            writer.WriteStartObject("items");
            writer.WriteString("type", "object");
            WriteProperties(writer, section);
            writer.WriteEndObject();

            return;
        }

        writer.WriteString("type", "object");
        WriteProperties(writer, section);
    }
}
