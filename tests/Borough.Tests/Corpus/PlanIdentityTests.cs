using System.Text.RegularExpressions;

namespace Borough.Tests.Corpus;

/// <summary>
/// A numbered document's own heading names the number its filename does.
/// </summary>
/// <remarks>
/// <para>
/// <b>Filed and paid 2026-08-19, by <c>plans/0033</c> having read <c># 0031</c> since the day it was
/// created.</b> Conserved Money was drafted as <c>0031</c>, parking took that number *first by
/// sixteen hours*, and the renumber moved the **filename** and the citations while leaving the
/// heading behind — pointing at
/// <see href="../../plans/0031-parking.md">a different, existing plan about a different milestone</see>.
/// </para>
/// <para>
/// ⚠ <b>Nothing could have caught it, and the reason is the interesting part.</b> Every corpus check
/// is document-to-document: citations resolve, links open, tables render, no registry figure appears
/// bare. A **self**-reference is none of those — the number in a heading is a claim about the file it
/// is already in, so no other document has to agree with it and no link has to open. ***A document's
/// own title is a second copy of its number***, which is
/// <see href="../../plans/0012-corpus-audit.md">the audit</see>'s <b>Cause 1</b> on a surface that
/// ledger had never counted as a copy at all.
/// </para>
/// <para>
/// <b>A heading with no number is not a claim, and this does not invent one.</b> <c>plans/0001</c> and
/// <c>plans/0025</c> both open with a bare title, which is a style question and not a contradiction.
/// The check fires only where a heading <em>states</em> a number — because the failure it exists for
/// is a number that is wrong, never a number that is missing.
/// </para>
/// </remarks>
public sealed class PlanIdentityTests
{
    /// <summary>A leading <c>0123</c> or <c>0123a</c>, in a filename or in a heading.</summary>
    private static readonly Regex Number = new(@"^#?\s*(\d{4}[a-z]?)\b", RegexOptions.Compiled);

    [Fact]
    public void A_numbered_documents_heading_names_its_own_number()
    {
        string root = RepoRoot();
        List<string> wrong = [];

        foreach (string directory in (string[])["plans", Path.Combine("docs", "adr")])
        {
            foreach (string path in Directory.EnumerateFiles(
                Path.Combine(root, directory), "*.md", SearchOption.TopDirectoryOnly))
            {
                Match file = Number.Match(Path.GetFileName(path));

                if (!file.Success)
                {
                    continue;
                }

                string heading = File.ReadLines(path).FirstOrDefault(string.Empty);

                if (!heading.StartsWith('#'))
                {
                    continue;
                }

                Match stated = Number.Match(heading);

                // A heading that states no number claims nothing, and inventing the claim here would
                // be this test choosing a house style rather than catching a contradiction.
                if (!stated.Success || stated.Groups[1].Value == file.Groups[1].Value)
                {
                    continue;
                }

                wrong.Add(
                    $"  {Path.GetRelativePath(root, path)} opens \"{heading.Trim()}\", so it calls "
                    + $"itself {stated.Groups[1].Value} and everything else calls it "
                    + $"{file.Groups[1].Value}.");
            }
        }

        Assert.True(
            wrong.Count == 0,
            "a numbered document disagrees with its own filename about which document it is. The "
            + "filename is what every citation in the corpus resolves through, so the heading is the "
            + "half that moves:\n"
            + string.Join('\n', wrong));
    }

    /// <summary>
    /// No two numbered documents claim the same ordinal.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Filed and paid 2026-08-24, on a sighting from the milestone 24 session</b> — which merged
    /// <c>main</c> into its branch and found <c>plans/0041</c> existing twice, under two different
    /// slugs, ***merged with no conflict because the filenames differed***. <c>plans/0012</c>
    /// <b>Cause 7</b> owns the full version.
    /// </para>
    /// <para>
    /// 🔴 <b>The test beside this one looks like the check for that and is not.</b>
    /// <see cref="A_numbered_documents_heading_names_its_own_number"/> compares each file to
    /// <em>itself</em>; two documents at one ordinal each agree with themselves and it cannot see the
    /// pair. ***A filename is not a declaration, so nothing declares it twice*** — which is why a
    /// duplicate <c>PurposeTag</c> is a build error (<c>CA1069</c>) and a duplicate ADR number is
    /// silence.
    /// </para>
    /// <para>
    /// ⚠ <b>The ADR half is the worse one and it is why this is a test rather than a convention.</b>
    /// An ADR is cited <em>by number, in prose</em> — measured at <b>6,592</b> bare-number citations
    /// across <b>162</b> files against 1,839 carrying a slug — so duplicating one makes thousands of
    /// standing sentences ambiguous while every link still resolves and every other check stays green.
    /// </para>
    /// <para>
    /// ⚠ <b>This catches a collision at the MERGE and not at the moment it is created</b>, because a
    /// check running on one branch cannot see the other branch's file. That moment is when it is cheap
    /// to fix, and it is reached by the sessions telling each other. ***This is the backstop; the
    /// message is the fix.***
    /// </para>
    /// <para>
    /// <b><c>0000</c> and <c>0000a</c> are different ordinals</b> and the suffix is part of the key —
    /// <c>plans/0000-board.md</c> and <c>plans/0000a-board-archive.md</c> both ship and neither is a
    /// duplicate of the other.
    /// </para>
    /// </remarks>
    [Fact]
    public void No_two_numbered_documents_claim_one_ordinal()
    {
        string root = RepoRoot();
        Dictionary<string, List<string>> byOrdinal = new(StringComparer.Ordinal);

        foreach (string directory in (string[])["plans", Path.Combine("docs", "adr")])
        {
            foreach (string path in Directory.EnumerateFiles(
                Path.Combine(root, directory), "*.md", SearchOption.TopDirectoryOnly))
            {
                Match file = Number.Match(Path.GetFileName(path));

                if (!file.Success)
                {
                    continue;
                }

                // Keyed on directory as well as ordinal: plans/0041 and adr/0041 are different
                // documents and always were. It is two files in ONE space that is the collision.
                string key = $"{directory}/{file.Groups[1].Value}";

                if (!byOrdinal.TryGetValue(key, out List<string>? claimants))
                {
                    claimants = [];
                    byOrdinal[key] = claimants;
                }

                claimants.Add(Path.GetRelativePath(root, path));
            }
        }

        // Sorted so the failure text is stable, and built by walking the ordered key list rather than
        // the Dictionary -- 05 section 4 lint 3 is about simulation code, and this is a test, but the
        // reason it exists is determinism of output and that applies here too.
        List<string> collisions = [];

        foreach (string key in byOrdinal.Keys.OrderBy(k => k, StringComparer.Ordinal))
        {
            List<string> claimants = byOrdinal[key];

            if (claimants.Count > 1)
            {
                collisions.Add(
                    $"  {key} is claimed by {claimants.Count}: "
                    + string.Join(", ", claimants.OrderBy(c => c, StringComparer.Ordinal)));
            }
        }

        Assert.True(
            collisions.Count == 0,
            "two numbered documents claim one ordinal. Every citation in this corpus resolves "
            + "through that number, and an ADR's is written into prose as bare text -- so one of "
            + "these makes every standing sentence naming it ambiguous, while every link still "
            + "opens and every other check stays green. Renumber the later one and sweep its "
            + "citations:\n"
            + string.Join('\n', collisions));
    }

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
}
