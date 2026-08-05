using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Borough.Analysers;

/// <summary>
/// <c>purpose_tag</c> uniqueness, checked where <c>05 §4</c> says it must be: at build time.
/// </summary>
/// <remarks>
/// <para>
/// <b>A test was not enough, and the reason is specific rather than procedural.</b> Every other rule
/// in this assembly describes a defect that something could eventually observe — a float diverges
/// across machines, a walked <c>Dictionary</c> reorders, a managed field shows up in a GC trace. A
/// reused <c>purpose_tag</c> has <em>no runtime symptom at all</em>. It throws nothing, trips no
/// invariant, and produces a city that is entirely plausible: the two mechanisms sharing the tag draw
/// the same value at the same coordinates forever, so a Household that chooses a job badly also
/// chooses a shop badly, always, and every aggregate over it looks like ordinary variance. There is
/// no observation that distinguishes the correlated world from the uncorrelated one.
/// </para>
/// <para>
/// A unit test therefore does not merely run late — it runs at the wrong <em>kind</em> of moment. It
/// catches a duplicate when somebody happens to run the suite, which may be after values have been
/// drawn under the wrong tag and after a State Hash baseline has been taken over them. The enum
/// declaration is the only place the defect is visible, and the compiler is the only thing that
/// reads it every time.
/// </para>
/// <para>
/// This replaces the stopgap <c>Borough.Tests.Determinism.PurposeTagTests</c>, which plans/0005
/// added knowing it was the wrong instrument and which its own doc comment asked to be deleted here.
/// </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PurposeTagAnalyser : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            Diagnostics.DuplicatePurposeTag,
            Diagnostics.PurposeTagZeroReserved,
            Diagnostics.PurposeTagUnderlyingType);

    private const string ReservedZeroName = "None";

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSymbolAction(AnalyseEnum, SymbolKind.NamedType);
    }

    private static void AnalyseEnum(SymbolAnalysisContext context)
    {
        var type = (INamedTypeSymbol)context.Symbol;

        if (type.TypeKind != TypeKind.Enum
            || type.ToDisplayString() != CoreConventions.PurposeTagEnum
            || !CoreConventions.IsReportableSource(type))
        {
            return;
        }

        if (type.EnumUnderlyingType?.SpecialType != SpecialType.System_UInt64)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.PurposeTagUnderlyingType, type.Locations[0],
                type.EnumUnderlyingType?.ToDisplayString() ?? "an unknown type"));
        }

        // A list and a linear scan rather than a dictionary: the enum is small, and the first name
        // to claim a value must be reported stably rather than in whatever order a hash produced.
        var claimed = new List<(object Value, string Name)>();

        foreach (IFieldSymbol member in type.GetMembers().OfType<IFieldSymbol>())
        {
            if (!member.HasConstantValue || member.ConstantValue is null)
            {
                continue;
            }

            object value = member.ConstantValue;
            Location location = member.Locations.FirstOrDefault(l => l.IsInSource) ?? type.Locations[0];

            if (IsZero(value) && member.Name != ReservedZeroName)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.PurposeTagZeroReserved, location, member.Name));
            }

            string? incumbent = claimed
                .FirstOrDefault(entry => Equals(entry.Value, value))
                .Name;

            if (incumbent is not null)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.DuplicatePurposeTag, location, member.Name, incumbent));
            }
            else
            {
                claimed.Add((value, member.Name));
            }
        }
    }

    /// <summary>
    /// The constant is boxed as whatever the underlying type is, which is <c>ulong</c> when the enum
    /// is declared correctly and something else exactly when <c>BOR0803</c> has already fired. It
    /// goes through <c>decimal</c> because that is the one conversion that cannot overflow for any
    /// legal underlying type, signed or unsigned.
    /// </summary>
    private static bool IsZero(object value) =>
        value is IConvertible convertible
        && convertible.ToDecimal(System.Globalization.CultureInfo.InvariantCulture) == 0m;
}
