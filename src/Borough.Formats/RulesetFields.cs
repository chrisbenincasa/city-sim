using System.Reflection;
using Borough.Core.Rules;

namespace Borough.Formats;

/// <summary>One stored value of a Ruleset, at the path the walk reached it by.</summary>
/// <param name="Path">Dotted field path from the root, with <c>[i]</c> for an array element.</param>
/// <param name="Value">The value, rendered invariantly.</param>
public readonly record struct RulesetField(string Path, string Value);

/// <summary>
/// A Ruleset flattened to its stored values, so two candidates can be compared without a
/// hand-written comparison per section.
/// </summary>
/// <remarks>
/// <para>
/// <b>Fields rather than properties, and that is what makes the walk terminate.</b> A Q16.16 value's
/// properties return the same type, so a property walk recurses forever; the stored state is the
/// fields. Private fields are included because a Ruleset keeps its tables in them.
/// </para>
/// <para>
/// Paths address dense ids, not typed source ids. <see cref="RulesetImpact"/> puts the authored id
/// back on a path before anybody reads it.
/// </para>
/// <para>
/// ⚠ <b>Reflection over <c>Borough.Core</c> is confined to this authoring path.</b> Nothing here
/// runs during a Tick, enters the State Hash or reaches a save; it answers what an edit did, before
/// a world exists.
/// </para>
/// </remarks>
public static class RulesetFields
{
    private const BindingFlags Stored = BindingFlags.Instance
        | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

    /// <summary>Every stored value of <paramref name="ruleset"/>, in walk order.</summary>
    public static IReadOnlyList<RulesetField> Of(Ruleset ruleset) => Of(ruleset, "ruleset");

    /// <summary>Every stored value of <paramref name="root"/>, addressed from <paramref name="path"/>.</summary>
    public static IReadOnlyList<RulesetField> Of(object root, string path)
    {
        ArgumentNullException.ThrowIfNull(root);

        var fields = new List<RulesetField>();
        Walk(root, path, fields, new HashSet<object>(ReferenceEqualityComparer.Instance));

        return fields;
    }

    /// <summary>
    /// An auto-property's field name as the property a reader knows, and any other field unchanged.
    /// </summary>
    private static string Spelled(string field) =>
        field.StartsWith('<') && field.EndsWith(">k__BackingField", StringComparison.Ordinal)
            ? field[1..field.IndexOf('>', StringComparison.Ordinal)]
            : field;

    private static void Walk(object? value, string path, List<RulesetField> fields, HashSet<object> seen)
    {
        if (value is null)
        {
            fields.Add(new RulesetField(path, "null"));
            return;
        }

        Type type = value.GetType();

        if (type.IsPrimitive || type.IsEnum || value is string or decimal)
        {
            fields.Add(new RulesetField(path, FormattableString.Invariant($"{value}")));
            return;
        }

        if (value is Delegate or MemberInfo or Pointer)
        {
            fields.Add(new RulesetField(path, type.Name));
            return;
        }

        if (!type.IsValueType && !seen.Add(value))
        {
            fields.Add(new RulesetField(path, "(seen)"));
            return;
        }

        if (value is Array array)
        {
            fields.Add(new RulesetField($"{path}.Length", array.Length.ToString(
                System.Globalization.CultureInfo.InvariantCulture)));
            int index = 0;

            foreach (object? element in array)
            {
                Walk(element, $"{path}[{index++}]", fields, seen);
            }

            return;
        }

        for (Type? level = type;
            level is not null && level != typeof(object) && level != typeof(ValueType);
            level = level.BaseType)
        {
            foreach (FieldInfo field in level.GetFields(Stored))
            {
                Walk(field.GetValue(value), $"{path}.{Spelled(field.Name)}", fields, seen);
            }
        }
    }
}
