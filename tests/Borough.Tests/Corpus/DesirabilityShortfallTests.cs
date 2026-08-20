using System;
using System.Collections.Generic;
using System.IO;

namespace Borough.Tests.Corpus;

/// <summary>
/// A document may say desirability is composed only if it also names amenity.
/// </summary>
/// <remarks>
/// <para>
/// <b>Scheduled by <c>adr/0123</c> and written by the task that made the claim true.</b> Milestone 9
/// composes two of desirability's four terms and leaves amenity out; the field is therefore
/// <b>bounded above by zero</b> and its maximum is clean, quiet, empty ground. That shortfall is a
/// <b>caveat</b>, and a caveat is the thing this corpus knows does not travel — <c>plans/0012</c>
/// <b>Cause 5</b> is the standing record of clauses staying behind while the claim moves on.
/// </para>
/// <para>
/// <b>This extends the disqualifier registry's idiom from a figure to a claim.</b> That registry
/// refuses a figure appearing without the phrase saying what it measures; this refuses the sentence
/// <em>desirability is composed</em> appearing without the term whose absence makes it partial.
/// </para>
/// <para>
/// ⚠ <b>It is written HERE and not when the ADR was taken.</b> Before this task no document claimed
/// the composition existed, so the test would have passed vacuously — and ***a vacuously-passing
/// obligation is an unread one***, which is milestone 7 task 8's finding.
/// </para>
/// <para>
/// ⚠ <b>DELETE THIS TEST AT MILESTONE 15.</b> Amenity arrives with a kind on a Business, the field
/// stops being bounded above by zero, and a check policing an absence outlives its subject the day the
/// absence ends. It is not a permanent rule about how desirability must be described.
/// </para>
/// <para>
/// ⚠ <b>The reach is deliberately loose and saying so is part of the check.</b> It asks only that the
/// word <c>amenity</c> appear somewhere in a document making the claim. That catches the real failure —
/// a sentence copied without its clause — and cannot catch a document that mentions amenity for an
/// unrelated reason. <b>A tighter proximity rule was available and refused</b>: it would fail on
/// ordinary rewording, and a check that cries wolf is a check somebody deletes.
/// </para>
/// </remarks>
public sealed class DesirabilityShortfallTests
{
    /// <summary>The sentences that constitute the claim, lowercased.</summary>
    private static readonly string[] Claims =
    [
        "desirability is composed",
        "desirability is built",
        "desirability composes",
        "composed desirability",
        "desirability stops throwing",
    ];

    [Fact]
    public void No_document_says_desirability_is_composed_without_naming_amenity()
    {
        var offenders = new List<string>();

        foreach (string path in ProseFiles(RepoRoot()))
        {
            string text = File.ReadAllText(path).ToLowerInvariant();
            string? claim = Array.Find(Claims, phrase => text.Contains(phrase, StringComparison.Ordinal));

            if (claim is not null && !text.Contains("amenity", StringComparison.Ordinal))
            {
                offenders.Add($"{Path.GetFileName(path)} says \"{claim}\"");
            }
        }

        Assert.True(
            offenders.Count == 0,
            "these documents claim desirability is composed and never name amenity: "
            + string.Join("; ", offenders)
            + ". Milestone 9 composes TWO of four terms and amenity — the only positive one — is absent, "
            + "so the field is bounded above by zero and its maximum is empty ground (adr/0123). A "
            + "reader taking the claim at face value concludes the field is finished. Name amenity's "
            + "absence beside the claim, or do not make the claim. This check is deleted at milestone "
            + "15, when amenity arrives and the absence ends.");
    }

    /// <summary>
    /// The check is not vacuous: something in the corpus does make the claim.
    /// </summary>
    /// <remarks>
    /// <b>The guard the parking long-run test taught this corpus to write.</b> Without it, a rewording
    /// that put every claim beyond the phrase list would leave this file green over a corpus it had
    /// stopped reading — and green is exactly what it looks like when it is working.
    /// </remarks>
    [Fact]
    public void Something_in_the_corpus_actually_makes_the_claim()
    {
        int claiming = 0;

        foreach (string path in ProseFiles(RepoRoot()))
        {
            string text = File.ReadAllText(path).ToLowerInvariant();

            if (Array.Exists(Claims, phrase => text.Contains(phrase, StringComparison.Ordinal)))
            {
                claiming++;
            }
        }

        Assert.True(
            claiming > 0,
            "no document makes the claim this test polices, so it passes over a corpus it is not "
            + "reading. Either the claim is worded differently now — add the wording to Claims — or "
            + "milestone 15 landed and this whole file should be deleted.");
    }

    private static IEnumerable<string> ProseFiles(string root)
    {
        foreach (string directory in new[] { "docs", "plans" })
        {
            foreach (string path in Directory.EnumerateFiles(
                Path.Combine(root, directory), "*.md", SearchOption.AllDirectories))
            {
                yield return path;
            }
        }

        foreach (string name in new[] { "CONTEXT.md", "PROCESS.md", "CLAUDE.md" })
        {
            string path = Path.Combine(root, name);

            if (File.Exists(path))
            {
                yield return path;
            }
        }
    }

    private static string RepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "docs")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new InvalidOperationException("no repository root above the test binary");
    }
}
