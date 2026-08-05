using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Borough.Analysers;

/// <summary>
/// <c>05 §4</c> lint 7 — every struct in Borough.Core satisfies the <c>unmanaged</c> constraint,
/// unless it is on the cold path and says so.
/// </summary>
/// <remarks>
/// <para>
/// <b>The rule is opt-out, and that is the decision.</b> The alternative — a <c>[SimulationState]</c>
/// marker checked only where applied — puts the rule's coverage in the hands of whoever remembers
/// the attribute, and a forgotten marker is a silent exemption with no diagnostic anywhere. Under
/// opt-out the friction lands on the *exception* instead, which is where the corpus wants it:
/// adr/0031 found its real axis by enumerating exceptions rather than granting them one at a time.
/// It also means the rule bites slice 4's row types the moment they are declared, with nothing to
/// remember.
/// </para>
/// <para>
/// <b>The exception is the hot/cold axis, not a list of type names.</b> <c>[ColdPath("why")]</c>
/// states the axis adr/0002 already established and adr/0036 owed an enumeration of: the hot path
/// allocates nothing and holds no references because it runs every Tick at the 1M target; the cold
/// path may do both, because it runs on a click. The two owed exceptions — the Ruleset interpreter
/// and the <c>Evidence</c> surface — are both cold by that test, and anything else claiming the
/// exception has to pass the same test out loud, in source, at the point of use.
/// </para>
/// <para>
/// <b>What this protects is not the tables.</b> Arrays of unmanaged structs are opaque to the GC, so
/// the tables were never at risk. The risk is the three derived, variable-length, per-entity
/// structures — per-Bin wait lists, cached Parking Sheds, Event Wheel buckets — which as collection
/// objects would be on the order of a million long-lived traced references. That is why the
/// companion rule is that every variable-length collection in the core is an intrusive index list.
/// </para>
/// <para>
/// <c>ref struct</c>s are skipped. One cannot be a field, an array element or a generic argument, so
/// it cannot be simulation state; <c>Span&lt;T&gt;</c> is the idiom adr/0036 names as C#'s systems
/// dialect and banning it here would ban the dialect.
/// </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UnmanagedStateAnalyser : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Diagnostics.ManagedState);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSymbolAction(AnalyseType, SymbolKind.NamedType);
    }

    private static void AnalyseType(SymbolAnalysisContext context)
    {
        var type = (INamedTypeSymbol)context.Symbol;

        if (type.TypeKind != TypeKind.Struct
            || type.IsRefLikeType
            || !CoreConventions.IsReportableSource(type)
            || type.IsUnmanagedType
            || IsColdPath(type))
        {
            return;
        }

        string owner = type.Name;
        bool reported = false;

        foreach (IFieldSymbol field in type.GetMembers().OfType<IFieldSymbol>())
        {
            if (field.IsStatic || field.Type.IsUnmanagedType)
            {
                continue;
            }

            // A record struct's backing fields are implicit; point at the positional parameter that
            // produced them rather than at nothing.
            Location location = field.Locations.FirstOrDefault(l => l.IsInSource)
                ?? type.Locations[0];

            context.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.ManagedState, location,
                field.Name, field.Type.ToDisplayString(), owner));
            reported = true;
        }

        // An open generic with an unconstrained parameter has no offending field to point at; the
        // missing `where T : unmanaged` is the defect and the type declaration is where it goes.
        if (!reported)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.ManagedState, type.Locations[0],
                "a type parameter", "unconstrained", owner));
        }
    }

    /// <summary>
    /// The type's own attribute, and nothing else. Walking outward to containing types would let a
    /// struct nested inside a cold one inherit the exception without stating a reason — which
    /// contradicts <c>ColdPathAttribute</c>'s own <c>Inherited = false</c>, and contradicts the point
    /// of requiring the reason at all. Nesting is a plausible way to organise Ruleset and Evidence
    /// types, so this would have been reached rather than being a hypothetical.
    /// </summary>
    private static bool IsColdPath(INamedTypeSymbol type) =>
        type.GetAttributes().Any(a =>
            a.AttributeClass?.ToDisplayString() == CoreConventions.ColdPathAttribute);
}
