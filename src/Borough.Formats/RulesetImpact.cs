using System.Text;
using Borough.Core.Determinism;
using Borough.Core.Rules;
using Borough.Core.Tables;

namespace Borough.Formats;

/// <summary>What became of one declaration between two candidates.</summary>
public enum RulesetImpactVerdict
{
    /// <summary>The candidate being compared against does not declare it.</summary>
    Added,

    /// <summary>The replacement does not declare it.</summary>
    Removed,

    /// <summary>Its values are identical and only its display label moved.</summary>
    Relabelled,

    /// <summary>At least one effective value moved.</summary>
    Changed,

    /// <summary>Nothing about it moved.</summary>
    Unchanged,
}

/// <summary>One effective value and what it was before.</summary>
/// <param name="Path">Where the value sits, relative to its declaration.</param>
/// <param name="Before">The old value, or null where the declaration is new.</param>
/// <param name="After">The new value, or null where the declaration is gone.</param>
public readonly record struct RulesetImpactValue(string Path, string? Before, string? After);

/// <summary>One declaration's fate, with every effective value that moved under it.</summary>
/// <param name="Section">The declaring section.</param>
/// <param name="Id">The typed source id, which is what identifies it across the two candidates.</param>
/// <param name="Verdict">What became of it.</param>
/// <param name="Label">Its display label in the replacement, or the old one where it is gone.</param>
/// <param name="WasLabelled">Its display label before, which differs when it was relabelled.</param>
/// <param name="Values">The values that moved, empty when nothing did.</param>
public sealed record RulesetImpactDeclaration(
    string Section,
    string Id,
    RulesetImpactVerdict Verdict,
    string? Label,
    string? WasLabelled,
    IReadOnlyList<RulesetImpactValue> Values);

/// <summary>One declaration that names a shared definition, and whether the edit reached it.</summary>
/// <param name="Section">The shared definition's section: <c>basket</c>, <c>recipe</c>, <c>reserve</c>.</param>
/// <param name="Id">The shared definition's typed id.</param>
/// <param name="BySection">The dependant declaration's section.</param>
/// <param name="ById">The dependant declaration's typed id.</param>
/// <param name="Key">The key that names the shared definition.</param>
/// <param name="Verdict">What became of the dependant.</param>
public sealed record RulesetImpactDependant(
    string Section,
    string Id,
    string BySection,
    string ById,
    string Key,
    RulesetImpactVerdict Verdict);

/// <summary>How many declarations a section holds on each side.</summary>
/// <param name="Section">The section.</param>
/// <param name="Before">How many the old candidate authored.</param>
/// <param name="After">How many the replacement authors.</param>
public readonly record struct RulesetImpactCount(string Section, int Before, int After);

/// <summary>
/// What replacing one resolved candidate with another would do, compared statically.
/// </summary>
/// <remarks>
/// <para>
/// <b>The comparison is of effective values, not of authored text.</b> A <c>[[basket]]</c>,
/// <c>[[recipe]]</c> or <c>[[reserve]]</c> expands inside the reader, so two candidates can carry
/// identical source for a Rule and still give it different terms. Comparing the resolved Rulesets
/// is the only way an edit to a shared definition shows up against the declarations it reaches.
/// </para>
/// <para>
/// <b>A value is addressed by its owning declaration's typed id.</b> Dense ids are allocated from
/// source order, so inserting a declaration renumbers every later one, and a comparison keyed by
/// dense id would report a whole Ruleset as changed. Term and Bin pools are re-read through their
/// owners for the same reason: a pool index is not stable across an edit, and the offsets that
/// address it are suppressed.
/// </para>
/// <para>
/// ⚠ <b>Some values belong to no one declaration and are reported against their field path.</b> The
/// tables a Resource is read through — needs, import prices, ceilings — are keyed by dense id inside
/// a world-level structure rather than held on the Resource, so an edit to one is reported at
/// <c>ruleset.ResourceNeeds...</c> rather than under <c>resource/food</c>. Nothing is dropped.
/// </para>
/// <para>
/// ⚠ <b>Without a world this is a static comparison and not a migration claim.</b> It says nothing
/// about stock, occupancy, solvency or whether a running city survives the transition.
/// </para>
/// </remarks>
public sealed class RulesetImpact
{
    private static readonly string[] Shared = ["basket", "recipe", "reserve"];

    private RulesetImpact(
        ulong before,
        ulong after,
        RulesetImpactDeclaration[] declarations,
        RulesetImpactValue[] world,
        RulesetImpactDependant[] dependants,
        RulesetImpactCount[] counts,
        RulesetDiagnostic[] diagnostics)
    {
        BeforeIdentity = before;
        AfterIdentity = after;
        Declarations = declarations;
        World = world;
        Dependants = dependants;
        Counts = counts;
        Diagnostics = diagnostics;
    }

    /// <summary>The old candidate's content identity, which binds this report to exact bytes.</summary>
    public ulong BeforeIdentity { get; }

    /// <summary>The replacement's content identity.</summary>
    public ulong AfterIdentity { get; }

    /// <summary>Every declaration on either side, sorted by section then id.</summary>
    public IReadOnlyList<RulesetImpactDeclaration> Declarations { get; }

    /// <summary>Values that belong to no one declaration, sorted by path.</summary>
    public IReadOnlyList<RulesetImpactValue> World { get; }

    /// <summary>Every dependant of a shared definition, sorted, with what the edit did to it.</summary>
    public IReadOnlyList<RulesetImpactDependant> Dependants { get; }

    /// <summary>Authored declarations per section, sorted by section.</summary>
    public IReadOnlyList<RulesetImpactCount> Counts { get; }

    /// <summary>Why no report can be made, sorted. Empty on success.</summary>
    public IReadOnlyList<RulesetDiagnostic> Diagnostics { get; }

    /// <summary>Whether the two candidates could be compared at all.</summary>
    public bool Ok => Diagnostics.Count == 0;

    /// <summary>Every reason there is no report, one per line.</summary>
    public string Describe() => string.Join(Environment.NewLine, Diagnostics);

    /// <summary>Whether anything at all would move.</summary>
    public bool Moves => Declarations.Any(d => d.Verdict != RulesetImpactVerdict.Unchanged)
        || World.Count > 0;

    /// <summary>
    /// Compares <paramref name="before"/> against <paramref name="after"/>. Both must have resolved:
    /// a refused candidate has no values to report and is itself the thing to fix.
    /// </summary>
    public static RulesetImpact Between(RulesetSourceResult before, RulesetSourceResult after)
    {
        ArgumentNullException.ThrowIfNull(before);
        ArgumentNullException.ThrowIfNull(after);

        var refusals = new List<RulesetDiagnostic>();

        Require(before, "the Ruleset being compared against", refusals);
        Require(after, "the replacement", refusals);
        Collide(before, after, refusals);

        if (refusals.Count > 0)
        {
            return new RulesetImpact(
                before.Capture?.ContentHash ?? 0,
                after.Capture?.ContentHash ?? 0,
                [], [], [], [],
                RulesetDiagnostic.Sorted(refusals));
        }

        Dictionary<(string Section, string Id), List<RulesetImpactValue>> was = Values(before);
        Dictionary<(string Section, string Id), List<RulesetImpactValue>> now = Values(after);

        var declarations = new List<RulesetImpactDeclaration>();

        foreach ((string Section, string Id) key in
            was.Keys.Union(now.Keys).OrderBy(k => k.Section, StringComparer.Ordinal)
                .ThenBy(k => k.Id, StringComparer.Ordinal))
        {
            declarations.Add(Compare(
                key,
                was.GetValueOrDefault(key),
                now.GetValueOrDefault(key),
                LabelOf(before, key),
                LabelOf(after, key)));
        }

        return new RulesetImpact(
            before.Capture?.ContentHash ?? 0,
            after.Capture?.ContentHash ?? 0,
            [.. declarations],
            [.. Moved(WorldOf(before), WorldOf(after))],
            [.. DependantsOf(after, declarations)],
            [.. CountsOf(before, after)],
            []);
    }

    private static void Require(RulesetSourceResult candidate, string which, List<RulesetDiagnostic> into)
    {
        if (candidate.Ruleset is null)
        {
            into.Add(new RulesetDiagnostic(
                candidate.Capture?.EntryName ?? "", 0, 0, RulesetDiagnosticCode.Ruleset, null, null,
                $"{which} was refused, so there is nothing to compare. Its own refusals say why."));
        }
    }

    /// <summary>
    /// Refuses two distinct ids that fold to one identity key, across both candidates together.
    /// </summary>
    /// <remarks>
    /// <b>Core addresses a declaration by <c>ContentHash.Of(UTF8(id))</c>, so a collision makes two
    /// declarations one thing.</b> Across a transition it would also silently carry a world's saved
    /// state from the old declaration onto the new one. Picking a winner is never right.
    /// </remarks>
    private static void Collide(
        RulesetSourceResult before, RulesetSourceResult after, List<RulesetDiagnostic> into)
    {
        var folded = new Dictionary<ulong, (string Section, string Id)>();

        foreach (RulesetSourceResult candidate in new[] { before, after })
        {
            foreach (RulesetSourceDeclaration declaration in candidate.Declarations)
            {
                if (declaration.Id is not { } id)
                {
                    continue;
                }

                ulong key = ContentHash.Of(Encoding.UTF8.GetBytes(id));

                if (folded.TryGetValue(key, out (string Section, string Id) held))
                {
                    if (!string.Equals(held.Id, id, StringComparison.Ordinal))
                    {
                        into.Add(new RulesetDiagnostic(
                            declaration.Location.Path, declaration.Location.Line,
                            declaration.Location.Column, RulesetDiagnosticCode.DuplicateId,
                            declaration.Section, id,
                            $"'{id}' and '{held.Id}' fold to the same identity key, so the two "
                            + "candidates cannot be compared and neither can replace the other. "
                            + "Core addresses a declaration by that key, and picking a winner "
                            + "would move one declaration's saved state onto the other."));
                    }

                    continue;
                }

                folded[key] = (declaration.Section, id);
            }
        }
    }

    private static string? LabelOf(RulesetSourceResult candidate, (string Section, string Id) key) =>
        candidate.Declarations.FirstOrDefault(
            d => d.Section == key.Section && d.Id == key.Id)?.Label;

    private static RulesetImpactDeclaration Compare(
        (string Section, string Id) key,
        List<RulesetImpactValue>? was,
        List<RulesetImpactValue>? now,
        string? wasLabelled,
        string? label)
    {
        if (was is null)
        {
            return new RulesetImpactDeclaration(
                key.Section, key.Id, RulesetImpactVerdict.Added, label, null,
                [.. now!.Select(v => v with { Before = null })]);
        }

        if (now is null)
        {
            return new RulesetImpactDeclaration(
                key.Section, key.Id, RulesetImpactVerdict.Removed, null, wasLabelled,
                [.. was.Select(v => v with { After = null })]);
        }

        RulesetImpactValue[] moved = [.. Moved(was, now)];
        bool relabelled = !string.Equals(wasLabelled, label, StringComparison.Ordinal);

        RulesetImpactVerdict verdict = moved.Length > 0 ? RulesetImpactVerdict.Changed
            : relabelled ? RulesetImpactVerdict.Relabelled
            : RulesetImpactVerdict.Unchanged;

        return new RulesetImpactDeclaration(
            key.Section, key.Id, verdict, label, relabelled ? wasLabelled : null, moved);
    }

    private static IEnumerable<RulesetImpactValue> Moved(
        IEnumerable<RulesetImpactValue> was, IEnumerable<RulesetImpactValue> now)
    {
        Dictionary<string, string?> old = was.ToDictionary(v => v.Path, v => v.After);
        Dictionary<string, string?> fresh = now.ToDictionary(v => v.Path, v => v.After);

        foreach (string path in old.Keys.Union(fresh.Keys).OrderBy(p => p, StringComparer.Ordinal))
        {
            string? left = old.GetValueOrDefault(path);
            string? right = fresh.GetValueOrDefault(path);

            if (!string.Equals(left, right, StringComparison.Ordinal))
            {
                yield return new RulesetImpactValue(path, left, right);
            }
        }
    }

    /// <summary>
    /// Every dependant of a shared definition, grouped under the definition it names rather than
    /// under the declaration that names it, because the question is what one edit reaches.
    /// </summary>
    private static IEnumerable<RulesetImpactDependant> DependantsOf(
        RulesetSourceResult after, List<RulesetImpactDeclaration> declarations)
    {
        Dictionary<(string, string), RulesetImpactVerdict> verdicts = declarations.ToDictionary(
            d => (d.Section, d.Id), d => d.Verdict);

        return after.References
            .Where(e => Shared.Contains(e.Reference.Target) && e.Id is not null)
            .Select(e => new RulesetImpactDependant(
                e.Reference.Target,
                e.TargetId,
                e.Section,
                e.Id!,
                e.Reference.Key,
                verdicts.GetValueOrDefault((e.Section, e.Id!), RulesetImpactVerdict.Unchanged)))
            .OrderBy(d => d.Section, StringComparer.Ordinal)
            .ThenBy(d => d.Id, StringComparer.Ordinal)
            .ThenBy(d => d.BySection, StringComparer.Ordinal)
            .ThenBy(d => d.ById, StringComparer.Ordinal)
            .ThenBy(d => d.Key, StringComparer.Ordinal);
    }

    private static IEnumerable<RulesetImpactCount> CountsOf(
        RulesetSourceResult before, RulesetSourceResult after)
    {
        Dictionary<string, int> was = Sections(before);
        Dictionary<string, int> now = Sections(after);

        return was.Keys.Union(now.Keys).OrderBy(s => s, StringComparer.Ordinal).Select(
            section => new RulesetImpactCount(
                section, was.GetValueOrDefault(section), now.GetValueOrDefault(section)));
    }

    private static Dictionary<string, int> Sections(RulesetSourceResult candidate)
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (RulesetSourceDeclaration declaration in candidate.Declarations)
        {
            counts[declaration.Section] = counts.GetValueOrDefault(declaration.Section) + 1;
        }

        return counts;
    }

    /// <summary>The root arrays a dense id indexes, and the section that id belongs to.</summary>
    private static readonly (string Root, string Section)[] Attributed =
    [
        ("_resources", "resource"),
        ("ResourceNeeds", "resource"),
        ("ResourceShelfLives", "resource"),
        ("ResourceKeys", "resource"),
        ("_rules", "rule"),
        ("_kinds", "building"),
        ("KindKeys", "building"),
        ("BusinessKinds", "business"),
        ("BusinessKindKeys", "business"),
        ("LifeStages", "life_stage"),
        ("LifeStageKeys", "life_stage"),
        ("_zoneRules", "zone_rule"),
        ("Policies", "policy"),
        ("PolicyKeys", "policy"),
    ];

    /// <summary>
    /// The pools, whose indices shift when a term is added, and the offsets that address them. Both
    /// are re-read through their owners instead.
    /// </summary>
    private static readonly string[] Pooled =
        ["_inputs", "_outputs", "_emissions", "_bins", "_kindRules"];

    private static readonly string[] Offsets =
    [
        "InputFirst", "InputCount", "OutputFirst", "OutputCount", "EmissionFirst", "EmissionCount",
        "BinFirst", "BinCount", "RuleFirst", "RuleCount",
    ];

    /// <summary>Every effective value, gathered under the declaration that owns it.</summary>
    private static Dictionary<(string Section, string Id), List<RulesetImpactValue>> Values(
        RulesetSourceResult candidate)
    {
        Ruleset ruleset = candidate.Ruleset!;
        var owned = new Dictionary<(string, string), List<RulesetImpactValue>>();

        foreach (RulesetField field in RulesetFields.Of(ruleset))
        {
            if (Attribute(candidate, field) is ({ } section, { } id, { } path))
            {
                Bucket(owned, section, id).Add(new RulesetImpactValue(path, null, field.Value));
            }
        }

        Pools(candidate, owned);
        Spell(candidate.Ids, owned);

        foreach (RulesetSourceDeclaration declaration in candidate.Declarations)
        {
            if (declaration.Id is { } id && !owned.ContainsKey((declaration.Section, id)))
            {
                owned[(declaration.Section, id)] = [];
            }
        }

        return owned;
    }

    /// <summary>Values belonging to no declaration, addressed by their field path.</summary>
    private static IEnumerable<RulesetImpactValue> WorldOf(RulesetSourceResult candidate)
    {
        foreach (RulesetField field in RulesetFields.Of(candidate.Ruleset!))
        {
            if (Attribute(candidate, field) is null && !Suppressed(field.Path))
            {
                yield return new RulesetImpactValue(field.Path, null, field.Value);
            }
        }
    }

    /// <summary>Which declaration a field path belongs to, and its path under that declaration.</summary>
    private static (string Section, string Id, string Path)? Attribute(
        RulesetSourceResult candidate, RulesetField field)
    {
        string[] parts = field.Path.Split('.', 3);

        if (parts.Length < 2)
        {
            return null;
        }

        int bracket = parts[1].IndexOf('[', StringComparison.Ordinal);

        if (bracket < 0 || !parts[1].EndsWith(']'))
        {
            return null;
        }

        string root = parts[1][..bracket];
        string section = Attributed.FirstOrDefault(a => a.Root == root).Section;

        if (section is null
            || !int.TryParse(
                parts[1][(bracket + 1)..^1],
                System.Globalization.NumberStyles.None,
                System.Globalization.CultureInfo.InvariantCulture,
                out int index))
        {
            return null;
        }

        string path = parts.Length > 2 ? parts[2] : Leaf(root);

        return Offsets.Contains(path)
            ? null
            : (section, IdAt(candidate.Ids, section, index), path);
    }

    /// <summary>What a root array's element is, where the element is a bare value.</summary>
    private static string Leaf(string root) => root switch
    {
        "_resources" => "family",
        "ResourceNeeds" => "need",
        "ResourceKeys" or "KindKeys" or "BusinessKindKeys" or "LifeStageKeys" or "PolicyKeys" => "key",
        _ => root,
    };

    private static string IdAt(RulesetNames ids, string section, int index) => section switch
    {
        "resource" => ids.Resource(new ResourceId((ushort)(index + 1))),
        "rule" => ids.Rule(new RuleId((ushort)(index + 1))),
        "building" => ids.Kind((byte)(index + 1)),
        "business" => ids.BusinessKind((byte)(index + 1)),
        "life_stage" => ids.LifeStage((byte)(index + 1)),
        "zone_rule" => ids.ZoneRule(index),
        "policy" => ids.Policy(index),
        _ => null,
    } ?? index.ToString(System.Globalization.CultureInfo.InvariantCulture);

    private static bool Suppressed(string path)
    {
        string[] parts = path.Split('.', 3);

        if (parts.Length < 2)
        {
            return false;
        }

        int bracket = parts[1].IndexOf('[', StringComparison.Ordinal);
        string root = bracket < 0 ? parts[1] : parts[1][..bracket];

        // A count is reported as an added or removed declaration, not as a value that moved.
        return Pooled.Contains(root)
            || (parts.Length == 2 && parts[1] == "Length")
            || (bracket < 0 && parts.Length > 2 && parts[2] == "Length"
                && Attributed.Any(a => a.Root == root));
    }

    /// <summary>
    /// The Terms and Bins a declaration owns, read through it rather than out of the pool.
    /// </summary>
    private static void Pools(
        RulesetSourceResult candidate,
        Dictionary<(string, string), List<RulesetImpactValue>> owned)
    {
        Ruleset ruleset = candidate.Ruleset!;
        RulesetNames ids = candidate.Ids;

        for (ushort raw = 1; raw <= ruleset.RuleCount; raw++)
        {
            var id = new RuleId(raw);
            List<RulesetImpactValue> values = Bucket(owned, "rule", IdAt(ids, "rule", raw - 1));

            Terms(values, ruleset.Inputs(id), "inputs");
            Terms(values, ruleset.Outputs(id), "outputs");

            for (int i = 0; i < ruleset.Emissions(id).Length; i++)
            {
                Add(values, RulesetFields.Of(ruleset.Emissions(id)[i], $"emissions[{i}]"));
            }
        }

        for (byte kind = 1; kind <= ruleset.KindCount; kind++)
        {
            List<RulesetImpactValue> values = Bucket(owned, "building", IdAt(ids, "building", kind - 1));

            for (int i = 0; i < ruleset.BinsOf(kind).Length; i++)
            {
                Add(values, RulesetFields.Of(ruleset.BinsOf(kind)[i], $"bins[{i}]"));
            }

            // A Rule is named by its source id, because a dense id moves when a Rule is inserted.
            for (int i = 0; i < ruleset.RulesOf(kind).Length; i++)
            {
                values.Add(new RulesetImpactValue(
                    $"rules[{i}]", null, IdAt(ids, "rule", ruleset.RulesOf(kind)[i].Raw - 1)));
            }
        }
    }

    /// <summary>
    /// Puts the authored id back on a value that holds a dense one.
    /// </summary>
    /// <remarks>
    /// <b>A dense id is not what an author wrote and it moves when a declaration is inserted.</b>
    /// Reporting <c>Resource.Raw 2 -> 4</c> asks the reader to count declarations; reporting
    /// <c>sundries -> timber</c> answers. A lookup that finds nothing keeps the number, which is the
    /// honest answer for a slot no declaration fills.
    /// </remarks>
    private static void Spell(
        RulesetNames ids, Dictionary<(string, string), List<RulesetImpactValue>> owned)
    {
        foreach (List<RulesetImpactValue> values in owned.Values)
        {
            for (int i = 0; i < values.Count; i++)
            {
                if (Namespace(values[i].Path) is { } section
                    && ushort.TryParse(
                        values[i].After,
                        System.Globalization.NumberStyles.None,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out ushort raw)
                    && raw != 0
                    && Named(ids, section, raw) is { } id)
                {
                    values[i] = values[i] with { After = id };
                }
            }
        }
    }

    private static string? Namespace(string path) => path switch
    {
        "Kind" => "building",
        "OnFail.Raw" => "rule",
        "Reports.Raw" => "condition",
        _ => path.EndsWith("Resource.Raw", StringComparison.Ordinal) ? "resource" : null,
    };

    private static string? Named(RulesetNames ids, string section, ushort raw) => section switch
    {
        "resource" => ids.Resource(new ResourceId(raw)),
        "rule" => ids.Rule(new RuleId(raw)),
        "building" => ids.Kind((byte)raw),
        "condition" => ids.Condition(new ConditionId(raw)),
        _ => null,
    };

    private static void Terms(List<RulesetImpactValue> values, ReadOnlySpan<Term> terms, string path)
    {
        for (int i = 0; i < terms.Length; i++)
        {
            Add(values, RulesetFields.Of(terms[i], $"{path}[{i}]"));
        }
    }

    private static void Add(List<RulesetImpactValue> values, IReadOnlyList<RulesetField> fields)
    {
        foreach (RulesetField field in fields)
        {
            values.Add(new RulesetImpactValue(field.Path, null, field.Value));
        }
    }

    private static List<RulesetImpactValue> Bucket(
        Dictionary<(string, string), List<RulesetImpactValue>> owned, string section, string id)
    {
        if (!owned.TryGetValue((section, id), out List<RulesetImpactValue>? values))
        {
            owned[(section, id)] = values = [];
        }

        return values;
    }
}
