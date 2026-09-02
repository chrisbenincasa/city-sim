using Borough.Formats;

namespace Borough.Tests.Corpus;

/// <summary>
/// Every key the loader reads carries an authored sentence, and every authored sentence names a key
/// the loader reads.
/// </summary>
/// <remarks>
/// <para>
/// <b>The corpus's third document-to-<em>code</em> check</b>, after <c>RefusalCountTests</c> and
/// <c>RulesetSchemaTests</c>, and the only one whose subject is prose a person wrote.
/// <c>RulesetKeyNotes</c> is a hand-authored list, which is the shape <c>plans/0012</c> <b>Cause 1</b>
/// is about — ***every document that stores what another document owns drifted*** — so it ships with
/// the thing every drifted copy in that ledger lacked: something that compares it.
/// </para>
/// <para>
/// <b>It fails in both directions on purpose.</b> A key with no note is the drift that matters — a
/// reader lands, the reference silently stops being complete, and nothing says so. A note naming no
/// key is the other half: a reader is deleted or renamed and the sentence describing it stays,
/// which is how a reference starts describing a language nobody writes. ⚠ <b>Neither is catchable
/// by reading the reference</b>, because a document that is missing a page looks exactly like a
/// document.
/// </para>
/// <para>
/// ⚠ <b>The surface is bounded by the shipped Rulesets, and so is this check.</b> A reader only ever
/// asks a table that exists, so a section no file in <c>rulesets/</c> declares is invisible here and
/// this test passes over it in silence — <c>RulesetSchemaTests</c> carries the same hole for the
/// same reason. ***A note for such a key is therefore permitted rather than reported as extra***,
/// which is the one asymmetry below: an unmatched note is refused only when its <em>section</em> is
/// one the surface knows.
/// </para>
/// </remarks>
public sealed class RulesetKeyNoteTests
{
    [Fact]
    public void Every_key_the_loader_reads_carries_a_note()
    {
        SortedSet<string> surface = Surface();

        List<string> missing = surface
            .Where(key => !RulesetKeyNotes.All.ContainsKey(key))
            .ToList();

        Assert.True(
            missing.Count == 0,
            $"{missing.Count} Ruleset key(s) the loader reads carry no note in "
                + "RulesetKeyNotes. A key with no sentence is a key the generated reference lists "
                + "and does not explain, and nothing else in the build would have said so.\n\n"
                + string.Join("\n", missing.Select(key => $"  {key}"))
                + "\n\nWrite one line each in src/Borough.Formats/RulesetKeyNotes.cs saying what the "
                + "key DOES. The range belongs in the loader's refusal, not here.");
    }

    /// <summary>
    /// No note describes a key the loader stopped reading.
    /// </summary>
    /// <remarks>
    /// <b>Scoped to sections the surface knows</b>, per the class remark: a section no shipped
    /// Ruleset declares is invisible to <c>KeySurface</c>, so a note for one of its keys is
    /// unverifiable rather than wrong, and refusing it would make this test demand the deletion of a
    /// correct sentence.
    /// </remarks>
    [Fact]
    public void No_note_describes_a_key_the_loader_does_not_read()
    {
        SortedSet<string> surface = Surface();

        HashSet<string> sections = surface
            .Select(key => key[..key.LastIndexOf(' ')])
            .ToHashSet(StringComparer.Ordinal);

        List<string> stale = RulesetKeyNotes.All.Keys
            .Where(key => key.LastIndexOf(' ') > 0)
            .Where(key => sections.Contains(key[..key.LastIndexOf(' ')]))
            .Where(key => !surface.Contains(key))
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToList();

        Assert.True(
            stale.Count == 0,
            $"{stale.Count} note(s) in RulesetKeyNotes describe a key no reader asks for, in a "
                + "section the loader does read. Either the reader was renamed and the sentence was "
                + "not, or it was deleted and the sentence outlived it -- and a reference that "
                + "explains a key nobody may write is worse than one that omits it.\n\n"
                + string.Join("\n", stale.Select(key => $"  {key}")));
    }

    /// <summary>Every key the loader reads, unioned over every shipped Ruleset.</summary>
    private static SortedSet<string> Surface()
    {
        string folder = Path.Combine(RepoRoot(), "rulesets");
        var keys = new SortedSet<string>(StringComparer.Ordinal);

        foreach (string file in Directory.GetFiles(folder, "*.toml").OrderBy(p => p, StringComparer.Ordinal))
        {
            foreach ((string section, IReadOnlyDictionary<string, RulesetKeyKind> inside)
                in RulesetLoader.KeySurface(File.ReadAllText(file), Path.GetFileName(file)))
            {
                // A key above the first section header belongs to no table. The loader refuses it by
                // name and there is nowhere in a reference to put it -- SchemaDump drops it for the
                // same reason.
                if (!section.StartsWith('['))
                {
                    continue;
                }

                foreach (string key in inside.Keys)
                {
                    keys.Add($"{section} {key}");
                }
            }
        }

        return keys;
    }

    private static string RepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "CLAUDE.md")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new InvalidOperationException("no repository root above the test binary.");
    }
}
