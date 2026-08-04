using System.Reflection;

namespace Borough.Tests;

/// <summary>
/// The three checks from docs/05 §4 that are enforceable by reflection alone.
/// The remainder need a Roslyn analyser and are listed in plans/0006-analysers-and-lints.md.
/// </summary>
public class BoundaryTests
{
    private static readonly Assembly Core = typeof(Borough.Core.AssemblyMarker).Assembly;

    /// <summary>Lint 1 — no Godot reference from Borough.Core, transitively. adr/0002</summary>
    [Fact]
    public void Core_does_not_reference_Godot()
    {
        var offenders = Core.GetReferencedAssemblies()
            .Where(a => a.Name!.StartsWith("Godot", StringComparison.Ordinal))
            .Select(a => a.Name!)
            .ToList();

        Assert.True(offenders.Count == 0,
            $"Borough.Core references {string.Join(", ", offenders)}. " +
            "adr/0002: the simulation is engine-agnostic and this boundary is the option " +
            "to abandon Godot at all.");
    }

    /// <summary>
    /// Lint 2, state half — no float or double in simulation state. adr/0003.
    /// Arithmetic is not covered here; that needs an analyser.
    /// </summary>
    [Fact]
    public void Core_has_no_floating_point_state()
    {
        var offenders =
            from type in Core.GetTypes()
            from field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic
                                       | BindingFlags.Instance | BindingFlags.Static)
            where field.FieldType == typeof(float)
               || field.FieldType == typeof(double)
               || field.FieldType == typeof(decimal)
            select $"{type.FullName}.{field.Name} ({field.FieldType.Name})";

        var found = offenders.ToList();

        Assert.True(found.Count == 0,
            $"Floating-point state in Borough.Core: {string.Join(", ", found)}. " +
            "adr/0003: integers and Q16.16 fixed-point only.");
    }

    /// <summary>
    /// adr/0002's second CI check — Core returns ids and numbers, never display text.
    /// The real leak vector was never `using Godot;`; it is a method that returns a
    /// formatted string because a panel wanted one.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Overrides of <c>object.ToString()</c> are exempt, and the exemption is narrow on purpose.</b>
    /// Slice 2 is the first code to land under this guard and it tripped it immediately: every
    /// `readonly record struct` — the shape plans/0005 prescribes for typed quantities — generates a
    /// public parameterless `ToString()`.
    /// </para>
    /// <para>
    /// The exemption is right rather than merely convenient. `object.ToString()` is callable on every
    /// type in .NET whether or not the type declares an override, so banning the override closes no
    /// leak; it only makes the string less useful, trading `Money { Raw = 5 }` for
    /// `Borough.Core.Quantities.Money`. Nothing about the boundary changes either way. What adr/0002
    /// actually names as the leak vector is a *bespoke* member — `GetBuildingName()`, a method that
    /// exists because a panel wanted one — and the guard below still catches every one of those.
    /// </para>
    /// <para>
    /// Anything wider than this exemption would be a hole. A `ToString(string format)` overload, a
    /// `Describe()`, or a string-returning property is not covered here and will still fail.
    /// </para>
    /// </remarks>
    [Fact]
    public void Core_returns_no_human_readable_strings()
    {
        var offenders =
            from type in Core.GetExportedTypes()
            from method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance
                                         | BindingFlags.Static | BindingFlags.DeclaredOnly)
            where method.ReturnType == typeof(string)
            where !method.IsSpecialName            // property getters are checked separately
            where !IsObjectToStringOverride(method)
            select $"{type.Name}.{method.Name}";

        var found = offenders.ToList();

        Assert.True(found.Count == 0,
            $"Public string-returning members on Borough.Core: {string.Join(", ", found)}. " +
            "adr/0002: the shell owns every string a human reads, resolved through the Ruleset.");
    }

    /// <summary>
    /// True only for a parameterless <c>ToString()</c> whose base definition is
    /// <see cref="object.ToString"/> — which is to say, the one the compiler writes for you.
    /// </summary>
    private static bool IsObjectToStringOverride(MethodInfo method) =>
        method.Name == nameof(ToString)
        && method.GetParameters().Length == 0
        && method.GetBaseDefinition().DeclaringType == typeof(object);
}
