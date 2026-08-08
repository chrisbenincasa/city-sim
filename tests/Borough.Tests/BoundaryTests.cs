using System.Reflection;

namespace Borough.Tests;

/// <summary>
/// The checks from docs/05 §4 and adr/0039 that are enforceable by reflection alone.
/// The remainder are Roslyn analysers in Borough.Analysers — see src/Borough.Analysers/Diagnostics.cs.
/// </summary>
public class BoundaryTests
{
    private static readonly Assembly Core = typeof(Borough.Core.AssemblyMarker).Assembly;
    private static readonly Assembly Formats = typeof(Borough.Formats.InputLogCodec).Assembly;

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

    /// <summary>
    /// adr/0039's layering: Core &lt;- Formats &lt;- the shells, and the arrows point one way.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>What this protects is the core's freedom from the filesystem</b> (<c>02 §1</c>) and from
    /// the strings a human reads (<c>adr/0002</c>). Borough.Formats exists to write both; a core that
    /// could name it would have acquired both by reference, and the reason the ADR gives for the
    /// fifth project — that nothing in it can reach the running simulation, because the simulation
    /// cannot name it — would stop being true.
    /// </para>
    /// <para>
    /// It is a cheap test for a failure that arrives by autocomplete rather than by decision. Nobody
    /// will argue for this reference; somebody will add it while reaching for a type.
    /// </para>
    /// </remarks>
    [Fact]
    public void Core_does_not_reference_Formats()
    {
        var offenders = Core.GetReferencedAssemblies()
            .Where(a => a.Name!.StartsWith("Borough.Formats", StringComparison.Ordinal))
            .Select(a => a.Name!)
            .ToList();

        Assert.True(offenders.Count == 0,
            "Borough.Core references Borough.Formats. adr/0039: the arrow points the other way, " +
            "and it is what keeps the core free of the filesystem and of human-readable strings.");
    }

    /// <summary>
    /// adr/0048's central claim: the TOML parser reaches Borough.Formats and stops there.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The ADR argues that <c>adr/0003</c>'s exception machinery is <em>not</em> owed, and this is
    /// the sentence that argument rests on.</b> <c>0003</c> requires any <b>core</b> dependency to be
    /// argued explicitly, because a determinism liability entering the core is not recoverable.
    /// Tomlyn enters the fifth project instead — so what protects determinism is not an exception
    /// document but the fact that the core cannot name the parser at all.
    /// </para>
    /// <para>
    /// <b>It is the cheapest of the checks here and the one most likely to be needed.</b> Nobody will
    /// argue for adding a parser reference to <c>Borough.Core.csproj</c>; somebody will add it while
    /// reaching for a type, exactly as with the Formats reference above. The narrower rule the ADR
    /// actually states — <em>nothing but integers and strings crosses from the parser into the
    /// loader</em> — is not reflection-checkable, and this is the half that is.
    /// </para>
    /// </remarks>
    [Fact]
    public void Core_does_not_reference_a_toml_parser()
    {
        var offenders = Core.GetReferencedAssemblies()
            .Where(a => a.Name!.Contains("Toml", StringComparison.OrdinalIgnoreCase))
            .Select(a => a.Name!)
            .ToList();

        Assert.True(offenders.Count == 0,
            $"Borough.Core references {string.Join(", ", offenders)}. adr/0048: the Ruleset is " +
            "parsed and validated in Borough.Formats, and the core receives ids and integers. " +
            "A parser in the core would be the determinism liability adr/0003 requires an " +
            "argued exception for, and no such exception exists.");
    }

    /// <summary>
    /// Borough.Formats is engine-agnostic too, and for a sharper reason than the core is.
    /// </summary>
    /// <remarks>
    /// The format's entire purpose is that a log written by Borough.Godot replays in
    /// Borough.Headless. A Formats that referenced Godot could not be loaded by the headless runner
    /// at all, which would defeat the property in the most direct way available.
    /// </remarks>
    [Fact]
    public void Formats_does_not_reference_Godot()
    {
        var offenders = Formats.GetReferencedAssemblies()
            .Where(a => a.Name!.StartsWith("Godot", StringComparison.Ordinal))
            .Select(a => a.Name!)
            .ToList();

        Assert.True(offenders.Count == 0,
            $"Borough.Formats references {string.Join(", ", offenders)}. adr/0039: a log written " +
            "by the game must replay in the headless runner, which cannot load Godot.");
    }
}
