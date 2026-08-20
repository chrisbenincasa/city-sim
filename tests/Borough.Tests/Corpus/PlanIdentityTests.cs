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
