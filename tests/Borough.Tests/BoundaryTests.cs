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
    [Fact]
    public void Core_returns_no_human_readable_strings()
    {
        var offenders =
            from type in Core.GetExportedTypes()
            from method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance
                                         | BindingFlags.Static | BindingFlags.DeclaredOnly)
            where method.ReturnType == typeof(string)
            where !method.IsSpecialName            // property getters are checked separately
            select $"{type.Name}.{method.Name}";

        var found = offenders.ToList();

        Assert.True(found.Count == 0,
            $"Public string-returning members on Borough.Core: {string.Join(", ", found)}. " +
            "adr/0002: the shell owns every string a human reads, resolved through the Ruleset.");
    }
}
