using System.Reflection;
using System.Text.RegularExpressions;
using Borough.Core.Invariants;

namespace Borough.Tests.Corpus;

/// <summary>
/// <b><c>plans/0012</c> check 7 — an obligation with no member reads as absent rather than as owed.</b>
/// </summary>
/// <remarks>
/// <para>
/// Filed 2026-08-12 by session H off <c>adr/0084</c>, which found <i>parking occupancy is conserved</i>
/// specified in <b>four</b> documents and built in <b>none</b>. It is check 5's shape pointed at the
/// invariant tiers instead of the ADR directory, and the third observation of the same mechanism:
/// <c>HouseholdHomeExists</c> was reported by nothing and found only by an audit, and <c>adr/0033</c>'s
/// satisfiability invariant sat specified across three documents until somebody built it and it found a
/// live defect in the committed golden baseline within minutes.
/// </para>
/// <para>
/// <b>Two design constraints from the filing, both of which rule out the obvious implementation.</b>
/// The list must be <b>read from <c>02 §10</c></b> rather than mirrored in a hand-written array here —
/// a copy in the instrument is <c>plans/0012</c> <i>Cause 1</i> arriving inside the thing meant to
/// catch it. And it <b>must not force a member to be written early</b>: <c>adr/0084</c> finds that an
/// invariant over <em>absent</em> state cannot be written at all, so what is asserted is that the gap
/// is <b>declared</b>, never that it is closed.
/// </para>
/// <para>
/// <b>The convention that makes the first half mechanical: <c>02 §10</c>'s tier table names each
/// invariant's enum member in backticks.</b> That is checked here rather than assumed, and it is what
/// closes the hole — a member that does not exist cannot be named, so an obligation cannot be written
/// into the tier table and built nowhere without this failing.
/// </para>
/// </remarks>
public sealed class InvariantCoverageTests
{
    /// <summary>A backticked token that looks like a member name rather than a citation.</summary>
    /// <remarks>
    /// <c>O(1)</c>, <c>adr/0033</c>, <c>06</c> and <c>05 §60</c> all appear in these cells too. The
    /// filter is deliberately shape-based — PascalCase, letters only, more than one word — because a
    /// list of exceptions would be a second copy of the table.
    /// </remarks>
    private static readonly Regex Candidate = new(
        @"`([A-Z][a-z]+(?:[A-Z][a-z]+)+)`", RegexOptions.Compiled);

    /// <summary>
    /// <b>Every invariant <c>02 §10</c> names has a member of the enum.</b>
    /// </summary>
    [Fact]
    public void Every_invariant_the_testing_strategy_names_has_a_member()
    {
        HashSet<string> declared = Declared();
        var missing = new List<string>();

        foreach (Match match in Candidate.Matches(TierTable()))
        {
            string named = match.Groups[1].Value;

            if (!declared.Contains(named))
            {
                missing.Add(named);
            }
        }

        Assert.True(
            missing.Count == 0,
            $"`02 §10`'s tier table names invariants with no member of the Invariant enum: "
            + $"{string.Join(", ", missing.Distinct())}. plans/0012 check 7: *an obligation with no "
            + "member reads as absent rather than as owed*. Add the member — marked [Unbuilt] if it "
            + "cannot be implemented yet, which is a declaration of the gap and not a promise to close "
            + "it — or, if the document is naming something that is not an invariant, reword the cell.");
    }

    /// <summary>
    /// <b>Every member is live, retired, or declared unbuilt — exactly one of the three.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Live is <em>referenced anywhere in <c>src/</c></em> rather than <em>registered</em>, and the
    /// weaker test is the honest one.</b> Registration happens through several shapes —
    /// <c>Require</c> at a write site, <c>Register</c> into a tier, a sweep closure — and a check that
    /// understood only some of them would report a live invariant as missing, which trains people to
    /// suppress it. What it still catches is the case it was filed for: a member nothing anywhere
    /// mentions.
    /// </para>
    /// <para>
    /// ⚠ <b>An id is never reused, so retirement is a marking rather than a deletion</b> — an id
    /// travels in a crash artifact and a reused one cannot be un-reused. <c>[Obsolete]</c> is how the
    /// project already spells that, and <see cref="Invariant.HouseholdHomeExists"/> is the precedent.
    /// </para>
    /// </remarks>
    [Fact]
    public void Every_member_is_live_or_retired_or_declared_unbuilt()
    {
        string source = string.Join(
            "\n",
            Directory.EnumerateFiles(
                Path.Combine(RepoRoot(), "src"), "*.cs", SearchOption.AllDirectories)
                .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                .Select(File.ReadAllText));

        var unaccounted = new List<string>();
        var doubled = new List<string>();

        foreach (FieldInfo field in Members())
        {
            bool retired = field.GetCustomAttribute<ObsoleteAttribute>() is not null;
            UnbuiltAttribute? unbuilt = field.GetCustomAttribute<UnbuiltAttribute>();
            bool live = source.Contains($"Invariant.{field.Name}", StringComparison.Ordinal);

            if (unbuilt is not null && string.IsNullOrWhiteSpace(unbuilt.OwedBy))
            {
                unaccounted.Add($"{field.Name} is marked [Unbuilt] with nothing named as owing it");
                continue;
            }

            if (!live && !retired && unbuilt is null)
            {
                unaccounted.Add(
                    $"{field.Name} is declared, is referenced nowhere in src/, and is marked neither "
                    + "[Obsolete] nor [Unbuilt]");
            }

            if (live && unbuilt is not null)
            {
                doubled.Add(
                    $"{field.Name} is marked [Unbuilt(\"{unbuilt.OwedBy}\")] and is also referenced in "
                    + "src/, so the marking is stale");
            }
        }

        Assert.True(
            unaccounted.Count == 0 && doubled.Count == 0,
            "plans/0012 check 7: every Invariant member must be live, retired or a declared gap. "
            + $"{string.Join("; ", unaccounted.Concat(doubled))}. A member nothing references and "
            + "nothing marks is the failure this check was filed over — it reads as built to every "
            + "reader and as absent to every instrument. Mark it [Unbuilt(\"what owes it\")], or "
            + "[Obsolete] if its id is retired, or implement it.");
    }

    /// <summary>
    /// <b>Every declared gap is visible in the document that owns the tiers.</b>
    /// </summary>
    /// <remarks>
    /// The other direction of the first test, and it is the half that keeps <c>[Unbuilt]</c> honest: a
    /// gap declared only in the enum is a gap no reader of <c>02 §10</c> can see, which is the same
    /// invisibility in the opposite file. ***A declaration nobody reads is the state this check exists
    /// to end***, so the marking has to appear on both sides.
    /// </remarks>
    [Fact]
    public void Every_declared_gap_is_named_by_the_testing_strategy()
    {
        string table = TierTable();
        var hidden = Members()
            .Where(field => field.GetCustomAttribute<UnbuiltAttribute>() is not null)
            .Select(field => field.Name)
            .Where(name => !table.Contains($"`{name}`", StringComparison.Ordinal))
            .ToList();

        Assert.True(
            hidden.Count == 0,
            $"these invariants are marked [Unbuilt] but `02 §10`'s tier table does not name them: "
            + $"{string.Join(", ", hidden)}. A gap declared only in the enum is invisible to every "
            + "reader of the document that owns the tiers, which is the same failure this check "
            + "catches in the other direction. Name it in the tier it belongs to.");
    }

    /// <summary>The three tier rows of <c>02 §10</c>, which is where invariants are named.</summary>
    /// <remarks>
    /// Sliced from the heading to the paragraph after the table rather than read whole, so prose
    /// elsewhere in the section — <c>§11</c>'s open questions, the long-run bullets — cannot be
    /// mistaken for a tier row.
    /// </remarks>
    private static string TierTable()
    {
        string text = File.ReadAllText(
            Path.Combine(RepoRoot(), "docs", "02-simulation-model.md"));

        int start = text.IndexOf("## 10. Testing strategy", StringComparison.Ordinal);

        Assert.True(start >= 0, "`02` no longer has a section 10, so check 7 is reading nothing.");

        int table = text.IndexOf("| Tier | When | What |", start, StringComparison.Ordinal);

        Assert.True(table >= 0, "`02 §10` no longer has its tier table, so check 7 is reading nothing.");

        int end = text.IndexOf("\n\nThe rest:", table, StringComparison.Ordinal);

        Assert.True(end > table, "`02 §10`'s tier table no longer ends where check 7 expects.");

        return text[table..end];
    }

    private static IEnumerable<FieldInfo> Members() =>
        typeof(Invariant)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.Name != nameof(Invariant.None));

    private static HashSet<string> Declared() =>
        [.. Members().Select(field => field.Name)];

    /// <summary>Walks up from the test assembly until the repository root is found.</summary>
    private static string RepoRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory is not null
            && !File.Exists(Path.Combine(directory.FullName, "CLAUDE.md")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);

        return directory!.FullName;
    }
}
