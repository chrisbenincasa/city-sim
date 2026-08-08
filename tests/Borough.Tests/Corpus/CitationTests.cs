using System.Text.RegularExpressions;

namespace Borough.Tests.Corpus;

/// <summary>
/// The corpus's own consistency check, and the only one that runs.
/// </summary>
/// <remarks>
/// <b>An ADR nobody cites is a decision that governs nothing.</b> It reads as settled, it is quoted
/// back in conversation as settled, and no document is actually obeying it — which is strictly worse
/// than an open question, because an open question is visible in
/// <see href="../../plans/0002-open-questions.md">the ledger</see>.
/// <para>
/// This is <see href="../../plans/0012-corpus-audit.md">the audit</see>'s *Cause 2* made mechanical.
/// The sweep found <c>adr/0049</c> — a decision about apply counts settled during slice 7 — cited by
/// no document anywhere: not <c>CONTEXT.md</c>, not <c>docs/02</c>–<c>06</c>, not <c>plans/</c>, not
/// <c>src/</c>. Nothing detected it, because there was nothing that could.
/// </para>
/// <para>
/// The check is deliberately weak. It asserts that a citation <em>exists</em>, not that the citing
/// document says anything true about it — no test can check that. What it catches is the failure
/// that actually happened: an ADR written, registered, and then never propagated to the documents
/// its own Consequences section addresses.
/// </para>
/// </remarks>
public sealed class CitationTests
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

    /// <summary>Where a citation has to come from for the decision to be governing something.</summary>
    /// <remarks>
    /// <b>Neither <c>plans/</c> nor <c>src/</c> counts, and both exclusions were earned.</b> A plan is
    /// transient — it describes work in progress and closes when the work lands — so a citation from
    /// one says the decision was <em>noticed</em>, not that anything obeys it. A source comment is
    /// better evidence but still the wrong place: under <c>adr/0042</c> a <b>design document owns the
    /// mechanism</b> and everything else cites, so an ADR reachable only from a code comment is one
    /// the corpus has not absorbed.
    /// <para>
    /// Both exclusions were added after a run that passed and should not have. The first version
    /// counted <c>plans/</c> and went green on the audit's own debt ledger; the second counted
    /// <c>src/</c> and went green on a comment written the same afternoon. Tightening to
    /// documentation leaves exactly <b>two</b> of forty-nine ADRs failing, which is what makes this
    /// the right line rather than a strict one — the rule was checked against the corpus before it
    /// was imposed on it.
    /// </para>
    /// </remarks>
    private static IEnumerable<string> CitingFiles(string root)
    {
        string adrs = Path.Combine(root, "docs", "adr");

        foreach (string path in Directory.EnumerateFiles(
            Path.Combine(root, "docs"), "*.md", SearchOption.AllDirectories))
        {
            if (!path.StartsWith(adrs, StringComparison.Ordinal) && !IsBuildOutput(path, root))
            {
                yield return path;
            }
        }

        foreach (string name in (string[])["CONTEXT.md", "CLAUDE.md", "PROCESS.md"])
        {
            string path = Path.Combine(root, name);

            if (File.Exists(path))
            {
                yield return path;
            }
        }

    }

    private static bool IsBuildOutput(string path, string root)
    {
        string relative = Path.GetRelativePath(root, path);

        return relative.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
                   StringComparison.Ordinal)
            || relative.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}",
                   StringComparison.Ordinal);
    }

    [Fact]
    public void Every_adr_is_cited_by_something_outside_the_adr_directory()
    {
        string root = RepoRoot();

        string[] numbers =
        [
            .. Directory.EnumerateFiles(Path.Combine(root, "docs", "adr"), "????-*.md")
                .Select(path => Path.GetFileName(path)[..4])
                .Order(StringComparer.Ordinal),
        ];

        Assert.NotEmpty(numbers);

        var cited = new HashSet<string>(StringComparer.Ordinal);

        foreach (string path in CitingFiles(root))
        {
            foreach (Match match in Regex.Matches(
                File.ReadAllText(path), @"adr/(\d{4})", RegexOptions.None, TimeSpan.FromSeconds(5)))
            {
                cited.Add(match.Groups[1].Value);
            }
        }

        string[] orphaned = [.. numbers.Where(number => !cited.Contains(number))];

        Assert.True(
            orphaned.Length == 0,
            $"these ADRs are cited by no document outside docs/adr: {string.Join(", ", orphaned)}. "
            + "A decision nothing cites is a decision governing nothing — it reads as settled while "
            + "no document obeys it. Propagate it to the document its Consequences address, which "
            + "for a design rule is the design document that owns the mechanism (adr/0042). If it is "
            + "genuinely superseded, the superseding ADR cites it and this passes.");
    }
}
