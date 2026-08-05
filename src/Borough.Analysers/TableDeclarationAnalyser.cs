using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Borough.Analysers;

/// <summary>
/// <c>BOR0901</c> — a <c>[Table]</c> type may hold declared columns and its own <c>Rows</c>, and
/// nothing else.
/// </summary>
/// <remarks>
/// <para>
/// <b>Most of adr/0003's field rule enforces itself, and this covers the part that cannot.</b>
/// Declaring a column is what allocates it, so an undeclared column has no storage and cannot be
/// written to — the rule holds from the inside for anything that goes through <c>Rows</c>. What does
/// not go through <c>Rows</c> is a bare <c>int[]</c> written beside the columns, which is storage the
/// declaration never saw, and it is exactly as convenient to write as the declared form.
/// </para>
/// <para>
/// <b>The check is on fields rather than on properties, and auto-properties are covered by
/// that.</b> A property is a pair of methods; storage is a field, including the one the compiler
/// writes for <c>public Column&lt;Ticks&gt; NextEventTick { get; }</c>. Checking fields therefore
/// reaches both spellings, and the diagnostic is reported at the property when that is what produced
/// the field — pointing at a compiler-generated symbol teaches nobody anything.
/// </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TableDeclarationAnalyser : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Diagnostics.UndeclaredTableField);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSymbolAction(AnalyseType, SymbolKind.NamedType);
    }

    private static void AnalyseType(SymbolAnalysisContext context)
    {
        var type = (INamedTypeSymbol)context.Symbol;

        if (!CoreConventions.IsReportableSource(type) || !IsTable(type))
        {
            return;
        }

        foreach (IFieldSymbol field in type.GetMembers().OfType<IFieldSymbol>())
        {
            if (field.IsStatic || field.IsConst || IsDeclaredStorage(field.Type))
            {
                continue;
            }

            ISymbol reported = field.AssociatedSymbol ?? field;

            Location location = reported.Locations.FirstOrDefault(l => l.IsInSource)
                ?? type.Locations[0];

            context.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.UndeclaredTableField, location,
                reported.Name, field.Type.ToDisplayString()));
        }
    }

    private static bool IsTable(INamedTypeSymbol type) =>
        type.TypeKind == TypeKind.Class
        && type.GetAttributes().Any(a =>
            a.AttributeClass?.ToDisplayString() == CoreConventions.TableAttribute);

    /// <summary>
    /// True for a declared column, or for the table's own <c>Rows</c>.
    /// </summary>
    /// <remarks>
    /// Both checks are against the <em>non-generic</em> base, so <c>HandleColumn&lt;Building&gt;</c>
    /// and any future column shape are covered without being enumerated here. That matters: a list of
    /// permitted type names would have to be edited every time a column type is added, and a check
    /// nobody remembers to edit is a check that stops holding on the day it is needed.
    /// </remarks>
    private static bool IsDeclaredStorage(ITypeSymbol type)
    {
        for (INamedTypeSymbol? current = type as INamedTypeSymbol;
             current is not null;
             current = current.BaseType)
        {
            string name = current.OriginalDefinition.ToDisplayString();

            if (name == CoreConventions.ColumnBaseType || name == CoreConventions.RowsBaseType)
            {
                return true;
            }
        }

        return false;
    }
}
