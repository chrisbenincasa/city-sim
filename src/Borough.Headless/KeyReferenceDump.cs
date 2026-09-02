using System.Text;
using Borough.Formats;

namespace Borough.Headless;

/// <summary>
/// The Ruleset key surface written out as a reference page, one section at a time.
/// </summary>
/// <remarks>
/// <para>
/// <b><see cref="SchemaDump"/>'s twin, and it exists because the schema is read by an editor and
/// nobody reads an editor's autocomplete to learn a format.</b> The two share one source and differ
/// only in shape: the key set comes from <c>RulesetLoader.KeySurface</c>, the sentence comes from
/// <see cref="RulesetKeyNotes"/>, and neither is authored here.
/// </para>
/// <para>
/// 🔴 <b>The committed page is generated, and <c>RulesetReferenceTests</c> is what makes that
/// worth anything.</b> A reference of a format is the exact shape <c>plans/0012</c> <b>Cause 1</b>
/// records — ***every document that stores what another document owns drifted*** — and
/// <c>plans/0050</c> found the same failure one level down, where thirty Ruleset headers were a
/// second copy of the loader. A page nobody regenerates is a page that describes last month's
/// loader. So the page is a build artefact, the test refuses a stale one, and editing it by hand is
/// refused the same way editing the schema is.
/// </para>
/// <para>
/// ⚠ <b>It states no values and no defaults, deliberately.</b> The loader's refusal already carries
/// the range and delivers it at the moment an author is wrong, which is the only moment it helps;
/// each Ruleset's own header carries what that file demonstrates; <c>plans/0002</c> §D carries
/// whether a number is ratified. ***A reference that repeated any of those would be the copy this
/// approach exists to avoid***, and it would be the copy that drifts fastest, because a value moves
/// more often than a meaning.
/// </para>
/// <para>
/// ⚠ <b>The surface is bounded by the shipped Rulesets, so this page cannot be complete either.</b>
/// A reader only ever asks a table that exists; a section no file in <c>rulesets/</c> declares is
/// invisible to the loader's own record of what it read, and therefore to this. The page says so in
/// its own preamble rather than presenting itself as the whole language.
/// </para>
/// </remarks>
internal static class KeyReferenceDump
{
    public static int Print(Options options)
    {
        string folder = Path.GetDirectoryName(Path.GetFullPath(options.RulesetPaths[0])) ?? ".";
        string[] files = Directory.GetFiles(folder, "*.toml");

        if (files.Length == 0)
        {
            Console.Error.WriteLine(
                $"no .toml files in {folder}. The key surface is unioned across every Ruleset in "
                + "the folder holding --ruleset, because one file only ever declares the sections "
                + "it happens to demonstrate.");

            return 1;
        }

        Array.Sort(files, StringComparer.Ordinal);

        SortedDictionary<string, SortedDictionary<string, RulesetKeyKind>> surface = Union(files);
        int keys = 0;
        int explained = 0;

        foreach (KeyValuePair<string, SortedDictionary<string, RulesetKeyKind>> section in surface)
        {
            foreach (string key in section.Value.Keys)
            {
                keys++;

                if (RulesetKeyNotes.For(section.Key, key) is not null)
                {
                    explained++;
                }
            }
        }

        Console.Error.WriteLine(
            $"{files.Length} Ruleset(s) read from {folder}; {surface.Count} section(s), {keys} "
            + $"key(s), {explained} explained.");

        Console.Out.Write(Render(surface, keys));
        Console.Out.Flush();

        return 0;
    }

    /// <summary>
    /// The page this mode would write for one Ruleset folder, as a string.
    /// </summary>
    /// <remarks>
    /// <b>Split out of <see cref="Print"/> so <c>RulesetReferenceTests</c> can compare the committed
    /// file against a fresh render rather than against a re-implementation of one.</b> A test that
    /// rebuilt the page's shape in its own words would be a second copy of the generator, checked
    /// against the generator — which is the arrangement this whole approach exists to avoid, one
    /// level further out.
    /// </remarks>
    internal static string Page(string folder)
    {
        string[] files = Directory.GetFiles(folder, "*.toml");

        Array.Sort(files, StringComparer.Ordinal);

        SortedDictionary<string, SortedDictionary<string, RulesetKeyKind>> surface = Union(files);
        int keys = 0;

        foreach (KeyValuePair<string, SortedDictionary<string, RulesetKeyKind>> section in surface)
        {
            keys += section.Value.Count;
        }

        return Render(surface, keys);
    }

    /// <summary>Every key every reader asks for, unioned across the folder.</summary>
    /// <remarks>
    /// <b>A refused file still contributes</b>, on <see cref="SchemaDump"/>'s reasoning: every
    /// reader has run and recorded what it asked for by the time the loader returns, whatever it
    /// concluded about the file. A reference built only from files that load would lose exactly the
    /// sections a demonstration of a refusal exists to show.
    /// </remarks>
    private static SortedDictionary<string, SortedDictionary<string, RulesetKeyKind>> Union(
        string[] files)
    {
        var surface =
            new SortedDictionary<string, SortedDictionary<string, RulesetKeyKind>>(
                StringComparer.Ordinal);

        foreach (string file in files)
        {
            foreach ((string section, IReadOnlyDictionary<string, RulesetKeyKind> inside)
                in RulesetLoader.KeySurface(File.ReadAllText(file), Path.GetFileName(file)))
            {
                // A key above the first section header belongs to no table. The loader refuses it by
                // name and there is nowhere on a page to put it.
                if (!section.StartsWith('['))
                {
                    continue;
                }

                if (!surface.TryGetValue(section, out SortedDictionary<string, RulesetKeyKind>? into))
                {
                    into = new SortedDictionary<string, RulesetKeyKind>(StringComparer.Ordinal);
                    surface[section] = into;
                }

                foreach ((string key, RulesetKeyKind kind) in inside)
                {
                    // A key typed by one file and untyped by another keeps the type, which is how
                    // the schema unions and is what keeps the two pages agreeing.
                    if (!into.TryGetValue(key, out RulesetKeyKind already)
                        || already == RulesetKeyKind.Unknown)
                    {
                        into[key] = kind;
                    }
                }
            }
        }

        return surface;
    }

    private static string Render(
        SortedDictionary<string, SortedDictionary<string, RulesetKeyKind>> surface, int keys)
    {
        var page = new StringBuilder();

        page.Append("# The Ruleset key reference\n\n");
        page.Append(
            "**Generated from `RulesetLoader` by `--key-reference`. Do not edit it** — "
            + "`RulesetReferenceTests` fails when this file drifts from the loader, exactly as "
            + "`RulesetSchemaTests` does for `rulesets/ruleset.schema.json`. Regenerate with:\n\n");
        page.Append(
            "```\ndotnet run --project src/Borough.Headless -- \\\n"
            + "  --key-reference --ruleset rulesets/minimal.toml > docs/ruleset-reference.md\n```\n\n");

        page.Append("---\n\n");

        page.Append(
            "## What this is, and what it is not\n\n"
            + "**The key set is derived and the sentences are authored.** Which keys exist comes "
            + "from the loader's own record of what its readers asked for, so this page cannot "
            + "list a key the loader does not read. What each key *does* is written by hand in "
            + "`src/Borough.Formats/RulesetKeyNotes.cs`, and a test refuses both a key with no "
            + "sentence and a sentence with no key.\n\n"
            + "⚠ **It states no values, no defaults and no ranges.** The loader carries the range "
            + "and delivers it in the refusal, at the moment an author is wrong, which is the only "
            + "moment it helps. Each file in `rulesets/` carries its own header saying what that "
            + "file demonstrates. [`plans/0002`](../plans/0002-open-questions.md) §D carries "
            + "whether a number is ratified — and **nearly every number in this design is not**.\n\n"
            + "⚠ **It cannot be complete, and the reason is structural.** A reader only ever asks "
            + "for a table that exists, so a section no shipped Ruleset declares is invisible to "
            + "the loader's record and therefore to this page. What is listed here is read; what "
            + "is absent may still be read by a file nobody has written.\n\n"
            + "⚠ **A listed key is not necessarily a permitted one.** Keys the loader knows only "
            + "in order to refuse them by name — so an author is told where a key went rather than "
            + "that it is unknown — are subtracted from this surface and do not appear.\n\n"
            + "**The loader is the authority. This is orientation.**\n\n");

        page.Append("---\n\n");
        page.Append($"## The sections\n\n{surface.Count} sections, {keys} keys.\n\n");

        foreach (KeyValuePair<string, SortedDictionary<string, RulesetKeyKind>> section in surface)
        {
            page.Append($"- [`{section.Key}`](#{Anchor(section.Key)}) — {section.Value.Count} key")
                .Append(section.Value.Count == 1 ? string.Empty : "s")
                .Append('\n');
        }

        page.Append('\n');

        foreach (KeyValuePair<string, SortedDictionary<string, RulesetKeyKind>> section in surface)
        {
            page.Append("---\n\n");
            page.Append($"## `{section.Key}`\n\n");

            if (section.Key.StartsWith("[[", StringComparison.Ordinal))
            {
                page.Append(
                    "*An array of tables — a file may declare this more than once.*\n\n");
            }

            foreach (KeyValuePair<string, RulesetKeyKind> key in section.Value)
            {
                page.Append($"**`{key.Key}`** · *{Named(key.Value)}*\n\n");
                page.Append(
                    RulesetKeyNotes.For(section.Key, key.Key)
                    ?? "*(no note authored — `RulesetKeyNoteTests` should have caught this)*")
                    .Append("\n\n");
            }
        }

        return page.ToString();
    }

    /// <summary>
    /// A GitHub heading anchor for a section name.
    /// </summary>
    /// <remarks>
    /// <b>Lower-cased, with every character that is not a letter, a digit or a space dropped, and
    /// spaces becoming hyphens</b> — which is the rule GitHub applies, and the brackets and the
    /// backticks are exactly what it drops. A wrong anchor is a link that scrolls nowhere, and
    /// <c>CorpusLinkTests</c> does not read a generated file.
    /// </remarks>
    private static string Anchor(string section)
    {
        var anchor = new StringBuilder(section.Length);

        foreach (char letter in section.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(letter) || letter == '_')
            {
                anchor.Append(letter);
            }
            else if (letter == ' ')
            {
                anchor.Append('-');
            }
        }

        return anchor.ToString();
    }

    /// <summary>How a key's shape is spelled on the page.</summary>
    /// <remarks>
    /// <b><c>Unknown</c> is written <em>unasserted</em> rather than <em>any</em></b>, because it is
    /// not a type: it is a reader that wanted a line number and never said what it expected. Calling
    /// it <em>any</em> would read as a permission the loader does not actually grant.
    /// </remarks>
    private static string Named(RulesetKeyKind kind) => kind switch
    {
        RulesetKeyKind.Whole => "whole number",
        RulesetKeyKind.Quoted => "quoted string",
        RulesetKeyKind.Table => "inline table",
        RulesetKeyKind.Array => "array of inline tables",
        RulesetKeyKind.Numbers => "array of whole numbers",
        _ => "unasserted",
    };
}
