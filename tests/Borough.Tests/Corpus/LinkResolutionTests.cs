using System.Text.RegularExpressions;

namespace Borough.Tests.Corpus;

/// <summary>
/// Mechanical check 8 — <b>a relative link points at a file that exists</b>.
/// </summary>
/// <remarks>
/// <b>Nothing in this corpus checked this until 2026-08-13, and the corpus is held together almost
/// entirely by relative links.</b> The three checks that existed all look like they cover it and none
/// does: <see cref="CitationTests"/> matches the <b>regex</b> <c>adr/\d{4}</c> and never opens a target,
/// <see cref="CoverageMapTests"/> asserts a row exists, and <see cref="MarkdownStyleTests"/> asserts
/// tables render. <b>A link to a file that does not exist passed all three.</b>
/// <para>
/// <b>It was found by committing it.</b> A sitting wrote <c>adr/0017</c> as
/// <i>households-satisfice-they-do-not-optimise</i> — the ADR's <b>claim</b> rather than its
/// <b>filename</b> (<i>agents-satisfice-they-never-optimise</i>) — ran the suite, and watched it go
/// green. The same sitting had already told the user those tests meant <i>"citations resolve"</i>, which
/// is <c>adr/0093</c> arriving on a <b>test name</b>: the check was described from what it is called
/// rather than from what it opens.
/// </para>
/// <para>
/// <b>It was measured before it was proposed, because a check nobody can pass is a cleanup project
/// wearing a ratchet's clothes.</b> Across every markdown file in the tree: <b>4,064 relative links,
/// five dead — and four of those in a stale <c>.claude/worktrees/</c> copy</b>. The live corpus had
/// exactly <b>one</b>, <c>adr/0094:132</c> pointing at <c>../plans/0013-tick-budget.md</c> from inside
/// <c>docs/adr/</c>, one <c>../</c> short. So this goes green the day it is written and every future
/// breakage is a red build rather than an audit — which is the property <c>adr/0003</c>'s per-field
/// declaration has and <i>remember to check your links</i> does not.
/// </para>
/// <para>
/// <b>Two scope decisions, both earned from <see cref="CitationTests"/>' two false-green revisions.</b>
/// <b>The worktree exclusion is structural rather than a filter</b> — <see cref="CorpusFiles"/>
/// enumerates <c>docs/</c> and <c>plans/</c> from the repository root, so a stale corpus under
/// <c>.claude/worktrees/</c> is unreachable rather than skipped, and cannot be re-included by somebody
/// widening a predicate. And <b>the anchor is deliberately not checked</b>: <c>#a-heading</c> is a far
/// weaker claim than a file existing, its slugification is renderer-specific, and folding the two
/// together would make a strong check fail for a weak reason.
/// </para>
/// </remarks>
public sealed class LinkResolutionTests
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
    /// Every markdown file the corpus is written in, build output excluded.
    /// </summary>
    /// <remarks>
    /// <b>This enumerates the corpus rather than the tree, and that is what keeps
    /// <c>.claude/worktrees/</c> out.</b> A worktree holds a whole second copy of <c>docs/</c> and
    /// <c>plans/</c> whose links are correct relative to <em>its</em> root and which is somebody's
    /// in-flight work; four of the five dead links in the tree on the day this was written were in one.
    /// </remarks>
    private static IEnumerable<string> CorpusFiles(string root)
    {
        foreach (string directory in (string[])["docs", "plans"])
        {
            foreach (string path in Directory.EnumerateFiles(
                Path.Combine(root, directory), "*.md", SearchOption.AllDirectories))
            {
                if (!IsBuildOutput(path, root))
                {
                    yield return path;
                }
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

    /// <summary>An inline markdown link or image: the target is everything up to a space or a hash.</summary>
    private static readonly Regex InlineLink =
        new(@"\[[^\]]*\]\(\s*<?([^)>\s#]+)[^)]*\)", RegexOptions.Compiled, TimeSpan.FromSeconds(5));

    /// <summary>Drops fenced code blocks, whose contents are not rendered and may show example links.</summary>
    private static string WithoutFences(string text)
    {
        string[] lines = text.Split('\n');
        var kept = new List<string>(lines.Length);
        bool fenced = false;

        foreach (string line in lines)
        {
            if (line.TrimStart().StartsWith("```", StringComparison.Ordinal))
            {
                fenced = !fenced;
                kept.Add(string.Empty);
                continue;
            }

            kept.Add(fenced ? string.Empty : line);
        }

        return string.Join('\n', kept);
    }

    /// <summary>
    /// The link targets in one document that name a path on disk.
    /// </summary>
    /// <remarks>
    /// <b>Split out from the check so that the check has something to be tested against.</b>
    /// <c>CLAUDE.md</c> requires a diagnostic to ship with a test that writes the violation and watches
    /// it fire; a file-scanning assertion cannot do that without committing a broken document, so the
    /// extraction is a pure function over text and <see cref="The_check_reports_a_dead_link"/> exercises
    /// it directly.
    /// <para>
    /// Absolute URLs and bare anchors are not paths and are dropped here rather than in the caller, so
    /// that <em>what counts as a link to a file</em> has exactly one definition.
    /// </para>
    /// </remarks>
    internal static IEnumerable<string> PathTargets(string markdown)
    {
        foreach (Match match in InlineLink.Matches(WithoutFences(markdown)))
        {
            string target = match.Groups[1].Value;

            if (target.Length == 0
                || target.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || target.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                || target.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            yield return target;
        }
    }

    /// <summary>Whether a target resolves to a file or a directory, relative to its own document.</summary>
    private static bool Resolves(string documentDirectory, string target)
    {
        string combined = Path.GetFullPath(
            Path.Combine(documentDirectory, target.Replace('/', Path.DirectorySeparatorChar)));

        return File.Exists(combined) || Directory.Exists(combined);
    }

    /// <summary>
    /// <b>Every relative link in the corpus points at something that is there.</b>
    /// </summary>
    /// <remarks>
    /// The failure this catches is not carelessness — it is that an ADR is cited by its <b>claim</b>,
    /// which is what a reader remembers, while the file is named for a <em>version</em> of that claim
    /// which may since have been reworded. <c>adr/0017</c> is titled <i>Households and Businesses
    /// satisfice; they never optimise</i> and filed as <c>agents-satisfice-they-never-optimise</c>,
    /// and <c>CONTEXT.md</c> bans the word in the filename outright.
    /// </remarks>
    [Fact]
    public void Every_relative_link_resolves()
    {
        string root = RepoRoot();
        var dead = new List<string>();
        int checked_ = 0;

        foreach (string path in CorpusFiles(root))
        {
            string directory = Path.GetDirectoryName(path)!;

            foreach (string target in PathTargets(File.ReadAllText(path)))
            {
                checked_++;

                if (!Resolves(directory, target))
                {
                    dead.Add($"{Path.GetRelativePath(root, path)} -> {target}");
                }
            }
        }

        Assert.True(checked_ > 1000, $"only {checked_} links found; the corpus has thousands, so the "
            + "extraction has stopped matching rather than the corpus having shrunk.");

        Assert.True(
            dead.Count == 0,
            $"these links point at nothing ({checked_} checked):\n  " + string.Join("\n  ", dead)
            + "\n\nThe usual cause is citing an ADR by its *claim* rather than by its *filename* — the "
            + "two differ whenever a title was reworded after the file was named, and adr/0017 is the "
            + "standing example. The second cause is a wrong number of ../ segments: a link written in "
            + "docs/adr/ needs ../../plans/, not ../plans/. Run `ls docs/adr | grep ^NNNN` and paste "
            + "the real name. Anchors are not checked, so a #fragment is never the reason this failed.");
    }

    /// <summary>
    /// <b>The check reports a dead link and passes a live one</b> — the violation written and watched
    /// to fire, per <c>CLAUDE.md</c>.
    /// </summary>
    /// <remarks>
    /// It also pins the three exclusions that would otherwise be silently lost: an absolute URL, a bare
    /// anchor, and a link inside a fenced block are all <em>not</em> paths, and a check that started
    /// reporting them would be red for reasons that are not defects.
    /// </remarks>
    [Fact]
    public void The_check_reports_a_dead_link()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"borough-links-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);

        try
        {
            File.WriteAllText(Path.Combine(directory, "neighbour.md"), "# there");

            string markdown = string.Join('\n', (string[])
            [
                "[live](neighbour.md)",
                "[dead](0017-households-satisfice-they-do-not-optimise.md)",
                "[external](https://example.com/x.md)",
                "[anchor](#a-heading)",
                "[live with anchor](neighbour.md#there)",
                "```",
                "[fenced](nothing-here.md)",
                "```",
            ]);

            string[] targets = [.. PathTargets(markdown)];

            Assert.Equal(
                (string[])
                [
                    "neighbour.md",
                    "0017-households-satisfice-they-do-not-optimise.md",
                    "neighbour.md",
                ],
                targets);

            string[] dead = [.. targets.Where(target => !Resolves(directory, target))];

            Assert.Equal(
                (string[])["0017-households-satisfice-they-do-not-optimise.md"], dead);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
