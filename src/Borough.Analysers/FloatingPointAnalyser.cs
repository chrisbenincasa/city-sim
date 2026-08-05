using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Borough.Analysers;

/// <summary>
/// <c>05 §4</c> lint 2 — no <c>float</c>, <c>double</c> or <c>decimal</c> in Borough.Core, in state
/// <em>or</em> in arithmetic.
/// </summary>
/// <remarks>
/// <para>
/// The reflection test in <c>Borough.Tests.BoundaryTests</c> already covers declared fields and is
/// kept, because it is cheap and it holds against a reference the analyser never sees. What it
/// cannot see is a <b>temporary</b>: <c>int r = (int)(a * 1.5f)</c> stores no float and is exactly
/// as non-deterministic as one that does. That expression is the case that motivated widening the
/// rule, and it is the case this analyser exists for.
/// </para>
/// <para>
/// Declarations are checked by symbol action and expressions by operation action, so a single site
/// can report twice — a <c>double</c> local initialised from a literal is both a declaration and a
/// literal. That is left alone. Two errors naming the same defect costs nothing; suppressing one to
/// look tidy risks suppressing the wrong one.
/// </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class FloatingPointAnalyser : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Diagnostics.FloatingPoint);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSymbolAction(AnalyseField, SymbolKind.Field);
        context.RegisterSymbolAction(AnalyseProperty, SymbolKind.Property);
        context.RegisterSymbolAction(AnalyseMethod, SymbolKind.Method);

        context.RegisterOperationAction(AnalyseLocal, OperationKind.VariableDeclarator);
        context.RegisterOperationAction(AnalyseLiteral, OperationKind.Literal);
        context.RegisterOperationAction(AnalyseConversion, OperationKind.Conversion);

        // Neither a lambda nor a local function is a symbol RegisterSymbolAction ever visits, so
        // `Func<double, double> f = v => v` was invisible: no literal, no conversion, no declared
        // member. They are reached as operations instead.
        context.RegisterOperationAction(AnalyseNestedFunction,
            OperationKind.AnonymousFunction, OperationKind.LocalFunction);
    }

    private static void AnalyseNestedFunction(OperationAnalysisContext context)
    {
        IMethodSymbol symbol = context.Operation switch
        {
            IAnonymousFunctionOperation lambda => lambda.Symbol,
            ILocalFunctionOperation local => local.Symbol,
            _ => throw new InvalidOperationException("unreachable: registered for two kinds only"),
        };

        Location location = context.Operation.Syntax.GetLocation();

        if (IsFloatingPoint(symbol.ReturnType))
        {
            Report(context, location, "the return type of this function");
        }

        foreach (IParameterSymbol parameter in symbol.Parameters)
        {
            if (IsFloatingPoint(parameter.Type))
            {
                Report(context, location, $"the parameter '{parameter.Name}'");
            }
        }
    }

    private static void AnalyseField(SymbolAnalysisContext context)
    {
        var field = (IFieldSymbol)context.Symbol;
        if (CoreConventions.IsReportableSource(field) && IsFloatingPoint(field.Type))
        {
            Report(context, field.Locations[0], $"the field '{field.Name}'");
        }
    }

    private static void AnalyseProperty(SymbolAnalysisContext context)
    {
        var property = (IPropertySymbol)context.Symbol;
        if (CoreConventions.IsReportableSource(property) && IsFloatingPoint(property.Type))
        {
            Report(context, property.Locations[0], $"the property '{property.Name}'");
        }
    }

    private static void AnalyseMethod(SymbolAnalysisContext context)
    {
        var method = (IMethodSymbol)context.Symbol;
        if (!CoreConventions.IsReportableSource(method))
        {
            return;
        }

        if (IsFloatingPoint(method.ReturnType))
        {
            Report(context, method.Locations[0], $"the return type of '{method.Name}'");
        }

        foreach (IParameterSymbol parameter in method.Parameters)
        {
            if (CoreConventions.IsReportableSource(parameter) && IsFloatingPoint(parameter.Type))
            {
                Report(context, parameter.Locations[0], $"the parameter '{parameter.Name}'");
            }
        }
    }

    private static void AnalyseLocal(OperationAnalysisContext context)
    {
        var declarator = (IVariableDeclaratorOperation)context.Operation;
        if (IsFloatingPoint(declarator.Symbol.Type))
        {
            Report(context, declarator.Syntax.GetLocation(),
                $"the local '{declarator.Symbol.Name}'");
        }
    }

    private static void AnalyseLiteral(OperationAnalysisContext context)
    {
        if (IsFloatingPoint(context.Operation.Type))
        {
            Report(context, context.Operation.Syntax.GetLocation(), "this literal");
        }
    }

    private static void AnalyseConversion(OperationAnalysisContext context)
    {
        var conversion = (IConversionOperation)context.Operation;
        if (IsFloatingPoint(conversion.Type) || IsFloatingPoint(conversion.Operand.Type))
        {
            Report(context, conversion.Syntax.GetLocation(), "this conversion");
        }
    }

    /// <summary>
    /// Structural rather than keyword-shaped — see
    /// <see cref="CoreConventions.CarriesFloatingPoint(ITypeSymbol)"/>. <c>double[]</c>,
    /// <c>double?</c>, <c>List&lt;double&gt;</c>, <c>Func&lt;double, double&gt;</c> and
    /// <c>Vector2</c> are all floating-point state whatever the declaration looks like.
    /// </summary>
    private static bool IsFloatingPoint(ITypeSymbol? type) =>
        CoreConventions.CarriesFloatingPoint(type);

    private static void Report(SymbolAnalysisContext context, Location location, string site) =>
        context.ReportDiagnostic(Diagnostic.Create(Diagnostics.FloatingPoint, location, site));

    private static void Report(OperationAnalysisContext context, Location location, string site) =>
        context.ReportDiagnostic(Diagnostic.Create(Diagnostics.FloatingPoint, location, site));
}
