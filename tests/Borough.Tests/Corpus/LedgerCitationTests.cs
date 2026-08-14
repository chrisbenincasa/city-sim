using System.Text.RegularExpressions;

namespace Borough.Tests.Corpus;

/// <summary>
/// Mechanical check 9 — <b>an ADR that names a ledger entry by number is named back by that entry</b>.
/// </summary>
/// <remarks>
/// <b>This is <c>plans/0012</c> Cause 2 — a write that did not land — made mechanical.</b> An ADR
/// settles, promotes or half-closes a ledger entry and says so, by number; the entry is never touched;
/// and the ledger goes on inviting a session to reopen what is already decided. <c>adr/0047</c> did
/// exactly this in the most legible way available — it enumerated the four entries it closed, in itself,
/// by number — and five rows across two documents stayed stale for two days, one of them serving as
/// milestone 5c's gate.
/// <para>
/// <b>Both ends are documents, which is the property that makes it checkable at all.</b> Cause 4 —
/// a decision taken from a description of the <em>code</em> — cannot be checked this way and is why
/// <c>adr/0093</c> is a writing rule rather than a test.
/// </para>
/// <para>
/// <b>Measured before it was built, per <see cref="LinkResolutionTests"/>' precedent.</b> Twenty-four
/// numbered ledger references across the ADR corpus, <b>ten of them qualified by a named source</b>, of
/// which <b>eight passed and two were the same live defect</b> — <c>adr/0098</c> citing
/// <i><c>01 §8</c> ledger #3</i> for <i>is car ownership a choice?</i> when that is
/// <c>plans/0002</c>'s number and <c>01 §8</c>'s own third entry is <i>open map or progressive land
/// unlock</i>, closed two days earlier by <c>adr/0090</c>. The wrong citation had propagated to six
/// documents inside one day, and the entry it should have named still read <i>"Live, and
/// half-answered"</i> from session five.
/// </para>
/// <para>
/// <b>Why that defect could not announce itself, which is the finding worth keeping.</b>
/// <c>plans/0002</c> holds <b>two</b> numbered ledgers sharing one namespace — the four-entry map and
/// endgame list, and the <i>Design forks, by owner</i> list running to #29b — and <c>01 §8</c> holds a
/// third. So <i>ledger #3</i> resolves to a <b>real but different</b> question in each, and the
/// <i>Design forks</i> list groups its entries under the owning document's name, which is what invites a
/// reader to write <c>01 §8</c> in front of <c>plans/0002</c>'s number. That is <c>plans/0012</c>
/// <b>Cause 5</b> arriving on an <b>identifier</b> rather than on a quantity: a bare <c>#3</c> travels
/// freely and lands on something that exists.
/// </para>
/// <para>
/// <b>Two scope decisions, both earned from <see cref="LinkResolutionTests"/>.</b> <b>Only qualified
/// references are checked</b> — fourteen of the twenty-four name no source within reach of the number,
/// and given three ledgers sharing a namespace a bare <c>#N</c> is genuinely ambiguous rather than
/// merely terse, so resolving one would be guessing. And <b>an entry may be numbered in more than one
/// list within a source, and any of them citing the ADR is enough</b>: the check asks whether the write
/// landed, not which list it landed in, so the namespace collision above can never make it red for a
/// reason that is not a defect.
/// </para>
/// </remarks>
public sealed class LedgerCitationTests
{
    /// <summary>Walks up from the test assembly until the corpus is found.</summary>
    private static string RepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "docs", "adr")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            "no directory above the test assembly contains docs/adr. This test reads the corpus "
            + "from disk, so it cannot run from a detached output directory.");
    }

    /// <summary>
    /// A ledger this check can resolve a number against: the token citations write, and the text the
    /// numbered entries live in.
    /// </summary>
    /// <remarks>
    /// <b>The token is the disambiguator and it is the whole reason the source list is short.</b>
    /// A citation that does not name one of these within reach of its number is not resolvable by a
    /// machine, because the same number exists in all three.
    /// </remarks>
    private sealed record LedgerSource(string Token, string RelativePath, string? SectionHeading);

    private static readonly LedgerSource[] Sources =
    [
        new("plans/0002", Path.Combine("plans", "0002-open-questions.md"), null),
        new("01 §8", Path.Combine("docs", "01-player-experience.md"), "## 8."),
    ];

    /// <summary>A numbered entry at the start of a line, optionally under an <c>###</c> heading.</summary>
    /// <remarks>
    /// <c>plans/0002</c> writes its map-and-endgame ledger as <c>### 1.</c> headings and its
    /// <i>Design forks</i> ledger as a plain <c>3.</c> list, so both forms are one pattern. The suffix
    /// letter is matched because that ledger really does number entries <c>14c</c> and <c>29b</c>.
    /// </remarks>
    private static readonly Regex NumberedEntry =
        new(@"^(?:#{2,4} )?(\d+)[a-z]?\. ", RegexOptions.Compiled | RegexOptions.Multiline,
            TimeSpan.FromSeconds(5));

    /// <summary>
    /// A numbered ledger reference: a source token, then a number, within one line.
    /// </summary>
    /// <remarks>
    /// <b>The sixty-character window is what makes a reference <em>qualified</em>.</b> The corpus writes
    /// these as <c>`plans/0002` ledger #2</c> and as <c>`plans/0002` <b>ledger #2</b></c> with emphasis
    /// between, so the gap is real; beyond about that distance the token stops being the number's
    /// antecedent and starts being a coincidence in the same sentence.
    /// </remarks>
    private static readonly Regex QualifiedReference =
        new(@"(plans/0002|01 §8)\D{0,60}?\*{0,2}ledger\*{0,2} ?\*{0,2}#(\d+)",
            RegexOptions.Compiled, TimeSpan.FromSeconds(5));

    /// <summary>The text a source's numbered entries live in — the whole file, or one section of it.</summary>
    private static string SectionText(string fileText, string? sectionHeading)
    {
        if (sectionHeading is null)
        {
            return fileText;
        }

        int start = fileText.IndexOf(sectionHeading, StringComparison.Ordinal);

        if (start < 0)
        {
            throw new InvalidOperationException(
                $"the section '{sectionHeading}' has been renamed or removed. This check resolves "
                + "ledger numbers against it, so it cannot silently fall back to the whole file.");
        }

        Match next = Regex.Match(fileText[(start + sectionHeading.Length)..], @"^## ",
            RegexOptions.Multiline, TimeSpan.FromSeconds(5));

        return next.Success
            ? fileText.Substring(start, sectionHeading.Length + next.Index)
            : fileText[start..];
    }

    /// <summary>
    /// The bodies of every entry numbered <paramref name="number"/> in <paramref name="text"/>.
    /// </summary>
    /// <remarks>
    /// <b>Plural on purpose.</b> A source may number the same entry in two lists — <c>plans/0002</c>
    /// does — and the check's question is whether the write landed anywhere, so returning all of them
    /// and accepting any is the reading that cannot be red for a reason that is not a defect.
    /// </remarks>
    internal static IReadOnlyList<string> EntryBodies(string text, int number)
    {
        MatchCollection markers = NumberedEntry.Matches(text);
        var bodies = new List<string>();

        for (int i = 0; i < markers.Count; i++)
        {
            if (int.Parse(markers[i].Groups[1].Value) != number)
            {
                continue;
            }

            int start = markers[i].Index;
            int end = i + 1 < markers.Count ? markers[i + 1].Index : text.Length;
            bodies.Add(text[start..end]);
        }

        return bodies;
    }

    /// <summary>Every qualified ledger reference in one document, as (token, number) pairs.</summary>
    internal static IEnumerable<(string Token, int Number)> QualifiedReferences(string markdown)
    {
        foreach (Match match in QualifiedReference.Matches(markdown))
        {
            yield return (match.Groups[1].Value, int.Parse(match.Groups[2].Value));
        }
    }

    /// <summary>Whether an entry's body names the ADR, by citation or by filename.</summary>
    private static bool Cites(string body, string adrNumber) =>
        Regex.IsMatch(body, $@"adr[/-]{adrNumber}\b", RegexOptions.None, TimeSpan.FromSeconds(5));

    /// <summary>
    /// <b>Every ADR that names a ledger entry by number is named back by that entry.</b>
    /// </summary>
    /// <remarks>
    /// The failure this catches is not carelessness. Striking a row feels like finishing the job, so the
    /// document that <em>records</em> the decision is updated and the ledger that <em>schedules</em> it
    /// is not — and a ledger entry that still reads as open is an invitation to spend a sitting
    /// reopening something already settled. <c>adr/0047</c>'s four closures are the standing example and
    /// two of them cost milestone 5c two days.
    /// </remarks>
    [Fact]
    public void Every_numbered_ledger_reference_is_cited_back()
    {
        string root = RepoRoot();

        Dictionary<string, string> ledgerText = Sources.ToDictionary(
            source => source.Token,
            source => SectionText(
                File.ReadAllText(Path.Combine(root, source.RelativePath)), source.SectionHeading));

        var unlanded = new List<string>();
        int checked_ = 0;

        foreach (string path in Directory.EnumerateFiles(
            Path.Combine(root, "docs", "adr"), "*.md", SearchOption.TopDirectoryOnly).Order())
        {
            string adrNumber = Path.GetFileName(path)[..4];

            foreach ((string token, int number) in QualifiedReferences(File.ReadAllText(path)))
            {
                checked_++;

                IReadOnlyList<string> bodies = EntryBodies(ledgerText[token], number);

                if (bodies.Count == 0)
                {
                    unlanded.Add($"adr/{adrNumber} names {token} ledger #{number}, which does not exist");
                }
                else if (!bodies.Any(body => Cites(body, adrNumber)))
                {
                    unlanded.Add($"adr/{adrNumber} names {token} ledger #{number}, "
                        + "and that entry does not name it back");
                }
            }
        }

        Assert.True(checked_ >= 8, $"only {checked_} qualified ledger references found; there were ten "
            + "when this was written, so the extraction has stopped matching rather than the corpus "
            + "having stopped citing. Check that citations still write the source token — `plans/0002` "
            + "or `01 §8` — within reach of the number.");

        Assert.True(
            unlanded.Count == 0,
            $"these decisions were never written back to the ledger they settle ({checked_} checked):\n  "
            + string.Join("\n  ", unlanded)
            + "\n\nThe repair is in the LEDGER, not in the ADR: open the entry and record what the ADR "
            + "did to it, citing the ADR. Striking a row feels like finishing the job, which is why this "
            + "half is the half that gets skipped (plans/0012 Cause 2).\n\nIf instead the ADR named the "
            + "wrong ledger, fix the ADR: plans/0002 holds TWO numbered ledgers sharing one namespace "
            + "and 01 §8 holds a third, so a number that resolves is not thereby the number you meant.");
    }

    /// <summary>
    /// <b>The check reports an unlanded write and passes a landed one</b> — the violation written and
    /// watched to fire, per <c>CLAUDE.md</c>.
    /// </summary>
    /// <remarks>
    /// It also pins the two scope decisions, which would otherwise be lost silently: an <b>unqualified</b>
    /// <c>#N</c> is not a reference this check can resolve, and an entry numbered in <b>two</b> lists
    /// passes if <b>either</b> names the ADR back.
    /// </remarks>
    [Fact]
    public void The_check_reports_a_write_that_did_not_land()
    {
        const string ledger =
            """
            ### 1. How big is the map? — CLOSED, see adr/0089
            body of the first entry.

            ## Design forks, by owner

            1. Is the map open? — closed by adr/0090
            2. Is car ownership a choice? — Live, and half-answered.
            """;

        // Qualified references are extracted; a bare "#2" and a foreign token are not.
        (string Token, int Number)[] found = [.. QualifiedReferences(
            "`plans/0002` **ledger #1** closes. See ledger #2 and `plans/0099` ledger #7. "
            + "`01 §8` ledger #4 is open.")];

        Assert.Equal(2, found.Length);
        Assert.Equal(("plans/0002", 1), found[0]);
        Assert.Equal(("01 §8", 4), found[1]);

        // Entry 1 is numbered in both lists, and only one of them names each ADR — which is enough.
        IReadOnlyList<string> entryOne = EntryBodies(ledger, 1);

        Assert.Equal(2, entryOne.Count);
        Assert.Contains(entryOne, body => Cites(body, "0090"));
        Assert.Contains(entryOne, body => Cites(body, "0089"));

        // Entry 2 exists and names nobody back: this is the defect the check exists for.
        IReadOnlyList<string> entryTwo = EntryBodies(ledger, 2);

        Assert.Single(entryTwo);
        Assert.DoesNotContain(entryTwo, body => Cites(body, "0098"));

        // A number no list carries is a dangling reference rather than an unlanded write.
        Assert.Empty(EntryBodies(ledger, 3));
    }
}
