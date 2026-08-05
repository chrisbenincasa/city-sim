using Microsoft.CodeAnalysis;

namespace Borough.Tests;

/// <summary>
/// The metadata references a probe compilation is built against.
/// </summary>
/// <remarks>
/// Referencing only <c>System.Private.CoreLib</c> is not enough — the type forwards in
/// <c>System.Runtime</c> are what a <c>using</c> resolves against — so the set is taken from whatever
/// the test host itself was loaded with.
/// </remarks>
internal static class TestReferences
{
    /// <summary>The platform, and <c>Borough.Core</c> alongside it.</summary>
    internal static MetadataReference[] WithCore { get; } = Build(includeCore: true);

    /// <summary>
    /// The platform alone. Used by probes that <em>declare</em> a type Borough.Core also declares —
    /// the PurposeTag enum — where a reference would make every mention ambiguous.
    /// </summary>
    internal static MetadataReference[] PlatformOnly { get; } = Build(includeCore: false);

    private static MetadataReference[] Build(bool includeCore)
    {
        var platform = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? string.Empty;

        var paths = platform
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Where(p => p.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            .ToList();

        string core = typeof(Borough.Core.AssemblyMarker).Assembly.Location;

        if (includeCore)
        {
            if (!paths.Contains(core, StringComparer.OrdinalIgnoreCase))
            {
                paths.Add(core);
            }
        }
        else
        {
            paths.RemoveAll(p => string.Equals(p, core, StringComparison.OrdinalIgnoreCase));
        }

        return [.. paths.Select(p => (MetadataReference)MetadataReference.CreateFromFile(p))];
    }
}
