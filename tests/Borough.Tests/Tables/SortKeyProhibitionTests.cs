using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Borough.Tests.Tables;

/// <summary>
/// <c>05 §3</c>'s determinism rule, asserted the way the typed quantities are: the illegal spelling
/// must <b>fail to compile</b>.
/// </summary>
/// <remarks>
/// <para>
/// <b>A handle index must never be used as a sort key in simulation logic.</b> Indices are recycled
/// by the free list, so ordering by one means an unrelated demolition on the far side of the city can
/// silently change who wins a contested draw downtown. There is nothing at runtime that could catch
/// that: the sort succeeds, the city is plausible, and the two runs that disagree were seeded
/// identically.
/// </para>
/// <para>
/// So the rule is structural. <c>Handle&lt;T&gt;</c> implements no <c>IComparable</c>, declares no
/// relational operator, and keeps <c>Index</c> internal — and the stable key it points people at,
/// the monotonic id, is an ordinary <c>ulong</c> that sorts fine. These probes are what make that a
/// promise rather than a present tense.
/// </para>
/// </remarks>
public class SortKeyProhibitionTests
{
    [Theory]
    [InlineData("comparing two handles", "var _ = Left() < Right();")]
    [InlineData("comparing them by hand", "var _ = Left().CompareTo(Right());")]
    [InlineData("reading the index", "var _ = Left().Index;")]
    [InlineData("ordering by the index", "var _ = new[] { Left() }.OrderBy(h => h.Index).ToArray();")]
    public void Ordering_by_a_handle_does_not_compile(string description, string statement)
    {
        ImmutableArray<Diagnostic> errors = Compile(statement);

        Assert.False(
            errors.IsEmpty,
            $"'{description}' compiled, and it must not. 05 §3's sort-key prohibition is structural: " +
            "if this now works, somebody added the member without the argument, and the determinism " +
            "bug it enables has no runtime detector at all.");
    }

    /// <summary>
    /// The negative cases are only worth something if the sanctioned route still works.
    /// </summary>
    [Theory]
    [InlineData("equality", "var _ = Left() == Right();")]
    [InlineData("the unset check", "var _ = Left().IsNone;")]
    [InlineData("ordering by the monotonic id", "var _ = new ulong[] { 1, 2 }.OrderBy(id => id).ToArray();")]
    public void The_sanctioned_spellings_still_compile(string description, string statement)
    {
        ImmutableArray<Diagnostic> errors = Compile(statement);

        Assert.True(
            errors.IsEmpty,
            $"'{description}' must compile but did not: " +
            string.Join("; ", errors.Select(e => e.GetMessage())));
    }

    private static ImmutableArray<Diagnostic> Compile(string statement)
    {
        string source = $$"""
            using System.Linq;
            using Borough.Core.Entities;
            using Borough.Core.Tables;

            internal static class Probe
            {
                internal static Handle<Citizen> Left() => default;

                internal static Handle<Citizen> Right() => default;

                internal static void Run()
                {
                    {{statement}}
                }
            }
            """;

        var compilation = CSharpCompilation.Create(
            assemblyName: "SortKeyProbe",
            syntaxTrees: [CSharpSyntaxTree.ParseText(source)],
            references: TestReferences.WithCore,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        return compilation.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToImmutableArray();
    }
}
