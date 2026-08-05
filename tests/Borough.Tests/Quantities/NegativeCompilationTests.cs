using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Borough.Tests.Quantities;

/// <summary>
/// plans/0005's acceptance criterion: the illegal operations must <b>fail to compile</b>, asserted
/// by a negative-compilation test rather than by a comment claiming it.
/// </summary>
/// <remarks>
/// This is the whole argument for typed quantities. adr/0003's rule — fixed-point multiplication
/// operates on dimensionless ratios, never on absolute quantities — is either structural or it is a
/// convention, and a convention needs a type or a lint because it never survives on discipline. These
/// tests are what make it the former: they fail the build the day somebody adds the convenient
/// operator.
/// </remarks>
public class NegativeCompilationTests
{
    [Theory]
    // adr/0003: fixed x fixed is legal only on dimensionless ratios.
    [InlineData("Tiles x Tiles", "var _ = new Tiles(2) * new Tiles(3);")]
    [InlineData("SubTiles x SubTiles", "var _ = new SubTiles(2) * new SubTiles(3);")]
    [InlineData("Money x Money", "var _ = new Money(2) * new Money(3);")]
    // A duration is not a distance, however valid the syntax looks.
    [InlineData("Ticks + Tiles", "var _ = new Ticks(1) + new Tiles(1);")]
    [InlineData("Money + Tiles", "var _ = new Money(1) + new Tiles(1);")]
    [InlineData("Ratio + Tiles", "var _ = Ratio.One + new Tiles(1);")]
    // Unsigned subtraction is the wrap adr/0003's width argument does not cover.
    [InlineData("Ticks - Ticks", "var _ = new Ticks(2) - new Ticks(1);")]
    // A quantity must not silently become its representation.
    [InlineData("Money from int", "Money _ = 5;")]
    [InlineData("Tiles to int", "int _ = new Tiles(5);")]
    public void Illegal_arithmetic_does_not_compile(string description, string statement)
    {
        ImmutableArray<Diagnostic> errors = Compile(statement);

        Assert.False(
            errors.IsEmpty,
            $"'{description}' compiled, and it must not. adr/0003's arithmetic rules are structural, " +
            "not advisory — if this operator now exists, it was added without the argument.");
    }

    [Theory]
    [InlineData("Ratio x Ratio", "var _ = Ratio.One * Ratio.One;")]
    [InlineData("SubTiles x int", "var _ = new SubTiles(2) * 3;")]
    [InlineData("SubTiles x Ratio", "var _ = new SubTiles(2) * Ratio.One;")]
    [InlineData("Money x int", "var _ = new Money(2) * 3;")]
    [InlineData("Money - Money", "var _ = new Money(2) - new Money(3);")]
    [InlineData("Tiles + Tiles", "var _ = new Tiles(2) + new Tiles(3);")]
    public void Legal_arithmetic_still_compiles(string description, string statement)
    {
        ImmutableArray<Diagnostic> errors = Compile(statement);

        Assert.True(
            errors.IsEmpty,
            $"'{description}' must compile but did not: " +
            string.Join("; ", errors.Select(e => e.GetMessage())));
    }

    /// <summary>Compiles one statement against the real Borough.Core and returns its errors.</summary>
    private static ImmutableArray<Diagnostic> Compile(string statement)
    {
        string source = $$"""
            using Borough.Core.Quantities;

            internal static class Probe
            {
                internal static void Run()
                {
                    {{statement}}
                }
            }
            """;

        var compilation = CSharpCompilation.Create(
            assemblyName: "NegativeCompilationProbe",
            syntaxTrees: [CSharpSyntaxTree.ParseText(source)],
            references: TestReferences.WithCore,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        return compilation.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToImmutableArray();
    }
}
