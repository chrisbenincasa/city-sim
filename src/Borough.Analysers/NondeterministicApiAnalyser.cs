using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Borough.Analysers;

/// <summary>
/// The three inputs the Input Log does not carry: the wall clock, fresh entropy, and an identity
/// derived from where an object happened to land.
/// </summary>
/// <remarks>
/// <para>
/// <c>System.Random</c> is <c>05 §4</c> lint 3's second half; the clock and the unstable identities
/// are rows of the same section's banned-construct table. They are one analyser because they are one
/// failure: <b>a value that enters a Tick from outside the Input Log</b>. Replay equivalence is the
/// property all three break, and it is the property slice 5 will assert.
/// </para>
/// <para>
/// The default <c>GetHashCode</c> is the one worth reading twice. It compiles, it is spelled like a
/// hash, and it returns a different number for the same logical value in the next process — so any
/// structure keyed on it is silently reordered per run, which is the same shape as the
/// <c>Dictionary</c> rule and arrives by a different door.
/// </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NondeterministicApiAnalyser : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            Diagnostics.WallClock,
            Diagnostics.UnstableIdentity,
            Diagnostics.SystemRandom);

    private const string RandomType = "System.Random";

    private static readonly ImmutableHashSet<string> ClockTypes = ImmutableHashSet.Create(
        "System.DateTime",
        "System.DateTimeOffset",
        "System.TimeProvider",
        "System.Diagnostics.Stopwatch");

    private static readonly ImmutableHashSet<string> ClockMembers = ImmutableHashSet.Create(
        "System.Environment.TickCount",
        "System.Environment.TickCount64");

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterOperationAction(AnalyseMemberUse,
            OperationKind.Invocation,
            OperationKind.PropertyReference,
            OperationKind.FieldReference,
            OperationKind.ObjectCreation);

        context.RegisterSymbolAction(AnalyseField, SymbolKind.Field);
        context.RegisterSymbolAction(AnalyseProperty, SymbolKind.Property);
        context.RegisterSymbolAction(AnalyseMethod, SymbolKind.Method);
    }

    private static void AnalyseMemberUse(OperationAnalysisContext context)
    {
        ISymbol? member = context.Operation switch
        {
            IInvocationOperation invocation => invocation.TargetMethod,
            IPropertyReferenceOperation property => property.Property,
            IFieldReferenceOperation field => field.Field,
            IObjectCreationOperation creation => creation.Constructor,
            _ => null,
        };

        if (member is null)
        {
            return;
        }

        string? owner = CoreConventions.ContainingTypeName(member);
        if (owner is null)
        {
            return;
        }

        Location location = context.Operation.Syntax.GetLocation();
        string name = $"{member.ContainingType.Name}.{member.Name}";

        if (owner == RandomType)
        {
            context.ReportDiagnostic(Diagnostic.Create(Diagnostics.SystemRandom, location, name));
            return;
        }

        if (ClockTypes.Contains(owner) || ClockMembers.Contains($"{owner}.{member.Name}"))
        {
            context.ReportDiagnostic(Diagnostic.Create(Diagnostics.WallClock, location, name));
            return;
        }

        if (IsUnstableIdentity(member))
        {
            context.ReportDiagnostic(
                Diagnostic.Create(Diagnostics.UnstableIdentity, location, name));
        }
    }

    /// <summary>
    /// <c>Guid.NewGuid()</c>, and any <c>GetHashCode()</c> that resolved to the one on
    /// <see cref="object"/> or <see cref="System.ValueType"/> rather than to an override.
    /// </summary>
    private static bool IsUnstableIdentity(ISymbol member)
    {
        if (member.Name == "NewGuid" && CoreConventions.ContainingTypeName(member) == "System.Guid")
        {
            return true;
        }

        // System.HashCode is seeded from process entropy at static init, so every member of it is
        // as unstable as the default GetHashCode and reads far more like a deliberate choice.
        if (CoreConventions.ContainingTypeName(member) == "System.HashCode")
        {
            return true;
        }

        if (member is not IMethodSymbol { Name: "GetHashCode", Parameters.Length: 0 })
        {
            return false;
        }

        // String *overrides* GetHashCode, so the object/ValueType test below never saw it — and
        // randomised string hashing is the exact hazard BOR0301 exists to prevent. Letting its
        // direct spelling through would have been the rule contradicting itself.
        return member.ContainingType.SpecialType
            is SpecialType.System_Object or SpecialType.System_ValueType or SpecialType.System_String;
    }

    private static void AnalyseField(SymbolAnalysisContext context)
    {
        var field = (IFieldSymbol)context.Symbol;
        if (CoreConventions.IsReportableSource(field) && IsRandom(field.Type))
        {
            ReportRandom(context, field.Locations[0], $"the field '{field.Name}'");
        }
    }

    private static void AnalyseProperty(SymbolAnalysisContext context)
    {
        var property = (IPropertySymbol)context.Symbol;
        if (CoreConventions.IsReportableSource(property) && IsRandom(property.Type))
        {
            ReportRandom(context, property.Locations[0], $"the property '{property.Name}'");
        }
    }

    private static void AnalyseMethod(SymbolAnalysisContext context)
    {
        var method = (IMethodSymbol)context.Symbol;
        if (!CoreConventions.IsReportableSource(method))
        {
            return;
        }

        if (IsRandom(method.ReturnType))
        {
            ReportRandom(context, method.Locations[0], $"the return type of '{method.Name}'");
        }

        foreach (IParameterSymbol parameter in method.Parameters)
        {
            if (CoreConventions.IsReportableSource(parameter) && IsRandom(parameter.Type))
            {
                ReportRandom(context, parameter.Locations[0], $"the parameter '{parameter.Name}'");
            }
        }
    }

    private static bool IsRandom(ITypeSymbol? type) =>
        type is not null && type.OriginalDefinition.ToDisplayString() == RandomType;

    private static void ReportRandom(SymbolAnalysisContext context, Location location, string site) =>
        context.ReportDiagnostic(Diagnostic.Create(Diagnostics.SystemRandom, location, site));
}
