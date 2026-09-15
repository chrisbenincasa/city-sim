using System.Globalization;

namespace Borough.Formats;

/// <summary>
/// One located reason a Ruleset source candidate was refused.
/// </summary>
/// <remarks>
/// <para>
/// Positions are one-based and zero means unknown. Refusals reported by the single-file reader carry
/// a line but no column. A diagnostic that belongs to no member (a whole-document refusal, or a
/// manifest problem) names the entry file.
/// </para>
/// <para>
/// Reports are sorted by path, line, column and code (then section, id and reason as tie-breaks), so
/// a different filesystem enumeration of the same captured bytes produces the same report.
/// </para>
/// </remarks>
/// <param name="Path">The manifest, member path or single-file Ruleset the position is in.</param>
/// <param name="Line">One-based line, or zero.</param>
/// <param name="Column">One-based column, or zero.</param>
/// <param name="Code">A <see cref="RulesetDiagnosticCode"/> value.</param>
/// <param name="Section">
/// The declaration section in scope (<c>rule</c>, <c>capacity</c>, ...), or null. With a null
/// <paramref name="Id"/> it names a singleton section.
/// </param>
/// <param name="Id">The typed id (or single-file name) in scope, or null.</param>
/// <param name="Reason">What is wrong, in a sentence.</param>
public sealed record RulesetDiagnostic(
    string Path, int Line, int Column, string Code, string? Section, string? Id, string Reason)
{
    /// <inheritdoc/>
    public override string ToString()
    {
        string declaration = (Section, Id) switch
        {
            (null, null) => string.Empty,
            (null, { } id) => $"'{id}': ",
            ({ } section, null) => $"[{section}]: ",
            ({ } section, { } id) => $"[[{section}]] '{id}': ",
        };

        return string.Create(
            CultureInfo.InvariantCulture, $"{Path}:{Line}:{Column}: {declaration}{Reason} ({Code})");
    }

    internal static RulesetDiagnostic[] Sorted(IEnumerable<RulesetDiagnostic> diagnostics)
    {
        RulesetDiagnostic[] sorted = [.. diagnostics];
        Array.Sort(sorted, Compare);
        return sorted;
    }

    private static int Compare(RulesetDiagnostic a, RulesetDiagnostic b)
    {
        int order = string.CompareOrdinal(a.Path, b.Path);

        if (order == 0)
        {
            order = a.Line.CompareTo(b.Line);
        }

        if (order == 0)
        {
            order = a.Column.CompareTo(b.Column);
        }

        if (order == 0)
        {
            order = string.CompareOrdinal(a.Code, b.Code);
        }

        if (order == 0)
        {
            order = string.CompareOrdinal(a.Section, b.Section);
        }

        if (order == 0)
        {
            order = string.CompareOrdinal(a.Id, b.Id);
        }

        return order == 0 ? string.CompareOrdinal(a.Reason, b.Reason) : order;
    }
}

/// <summary>The stable codes a <see cref="RulesetDiagnostic"/> carries.</summary>
public static class RulesetDiagnosticCode
{
    /// <summary>TOML the parser could not read.</summary>
    public const string Syntax = "syntax";

    /// <summary>A manifest or member that is not valid UTF-8.</summary>
    public const string Utf8 = "utf8";

    /// <summary>A manifest holding something other than exactly <c>[source]</c> version and members.</summary>
    public const string Manifest = "manifest";

    /// <summary>A <c>[source]</c> version this build does not read.</summary>
    public const string Version = "version";

    /// <summary>A member path outside the portable path vocabulary.</summary>
    public const string MemberPath = "member-path";

    /// <summary>A listed member that does not exist.</summary>
    public const string MemberMissing = "member-missing";

    /// <summary>A member listed or supplied twice.</summary>
    public const string MemberDuplicate = "member-duplicate";

    /// <summary>A supplied entry the manifest does not list.</summary>
    public const string MemberUnlisted = "member-unlisted";

    /// <summary>A listed member that is not a readable regular file beneath the package root.</summary>
    public const string MemberRead = "member-read";

    /// <summary>Member content that is not a declaration: root keys, <c>[source]</c>, a reopened table.</summary>
    public const string MemberShape = "member-shape";

    /// <summary>A missing or malformed <c>id</c>, or <c>name</c> written where <c>id</c> replaces it.</summary>
    public const string Id = "id";

    /// <summary>A malformed <c>label</c>.</summary>
    public const string Label = "label";

    /// <summary>A malformed <c>order</c>, or one on a section ordered by id.</summary>
    public const string Order = "order";

    /// <summary>Two declarations of one section with one id.</summary>
    public const string DuplicateId = "duplicate-id";

    /// <summary>A singleton section declared in more than one place.</summary>
    public const string SingletonOwner = "singleton-owner";

    /// <summary>One section written both as a table and as an array of tables.</summary>
    public const string DeclarationKind = "declaration-kind";

    /// <summary>Source v1 syntax whose runtime or resolver support this build does not have yet.</summary>
    public const string Unimplemented = "unimplemented";

    /// <summary>A stored bundle whose entries or metadata do not describe one captured Ruleset.</summary>
    public const string Bundle = "bundle";

    /// <summary>A stored bundle written by an envelope, source or resolver version this build lacks.</summary>
    public const string BundleVersion = "bundle-version";

    /// <summary>A stored bundle whose content does not fold to the identity it records.</summary>
    public const string BundleIdentity = "bundle-identity";

    /// <summary>A refusal from the single-file Ruleset reader.</summary>
    public const string Ruleset = "ruleset";
}
