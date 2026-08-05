using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Borough.Analysers;

/// <summary>
/// The names and shapes these analysers agree on, spelled once.
/// </summary>
/// <remarks>
/// Everything here is matched by name rather than by symbol identity. The analyser assembly must not
/// reference <c>Borough.Core</c> — it is loaded into the compiler that is building it — so a string
/// is the only handle available. The strings are load-bearing and are listed together for that
/// reason.
/// </remarks>
internal static class CoreConventions
{
    /// <summary>
    /// The one namespace exempt from <see cref="Diagnostics.RawDivision"/> and
    /// <see cref="Diagnostics.NonConstantShift"/>, because it is where the stated-rounding and
    /// range-checked helpers are <em>implemented</em>. A rule cannot ban the construct its own
    /// replacement is made of.
    /// </summary>
    internal const string ArithmeticSubstrateNamespace = "Borough.Core.Arithmetic";

    /// <summary>
    /// The rule-7 exception, encoded as the hot/cold axis rather than as a list of type names.
    /// adr/0036 owed this enumeration and plans/0006 is where it was paid.
    /// </summary>
    internal const string ColdPathAttribute = "Borough.Core.ColdPathAttribute";

    /// <summary>The central enum whose uniqueness nothing at runtime can check.</summary>
    internal const string PurposeTagEnum = "Borough.Core.Determinism.PurposeTag";

    /// <summary>
    /// True when the symbol being analysed lives in the arithmetic substrate itself.
    /// </summary>
    /// <remarks>
    /// <b>Exact, not prefix.</b> A child namespace does not inherit the exemption, because inventing
    /// <c>Borough.Core.Arithmetic.Whatever</c> is precisely how a single visible exemption spreads by
    /// copy — which is the thing this being one namespace was supposed to prevent. The exemption is
    /// resolved through <c>ContainingSymbol</c> rather than through syntax, so it still holds inside
    /// a lambda or a local function declared in the substrate.
    /// </remarks>
    internal static bool IsArithmeticSubstrate(ISymbol? symbol) =>
        symbol?.ContainingNamespace is { IsGlobalNamespace: false } ns
        && ns.ToDisplayString() == ArithmeticSubstrateNamespace;

    /// <summary>
    /// Types whose floating-point nature is not visible in their fields. <see cref="System.Half"/>
    /// stores a <c>ushort</c> and <c>Vector&lt;T&gt;</c> is a JIT intrinsic with nothing to walk, so
    /// both would pass a purely structural check.
    /// </summary>
    private static readonly ImmutableHashSet<string> OpaqueFloatingPointTypes =
        ImmutableHashSet.Create("System.Half", "System.Numerics.Vector`1");

    /// <summary>
    /// True when <paramref name="type"/> is floating point, or reaches floating point through an
    /// array, a pointer, a generic type argument, or the fields of a struct.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The check has to be structural, because the rule is about arithmetic rather than about a
    /// keyword.</b> A predicate that only asked whether a type's <c>SpecialType</c> is
    /// <c>Single</c>, <c>Double</c> or <c>Decimal</c> leaves the rule open at four doors that are
    /// each easier to walk through than writing <c>double</c>: <c>List&lt;double&gt;</c> and
    /// <c>Func&lt;double, double&gt;</c> hide it in a type argument, <c>double?</c> hides it in
    /// <see cref="System.Nullable{T}"/>, and <c>Vector2</c> hides it in two <c>float</c> fields
    /// while looking exactly like the type somebody writing a position would reach for.
    /// </para>
    /// <para>
    /// <b>Only structs are walked field-wise, and that boundary is the point.</b> Following the
    /// fields of a class would reach floating point from nearly every type in .NET within a few
    /// hops and the rule would report everything, which is the same as reporting nothing. A struct
    /// <em>is</em> its fields — that is what makes it storable as simulation state and what makes
    /// the question meaningful.
    /// </para>
    /// </remarks>
    internal static bool CarriesFloatingPoint(ITypeSymbol? type) => CarriesFloatingPoint(type, depth: 0);

    private static bool CarriesFloatingPoint(ITypeSymbol? type, int depth)
    {
        // Generic instantiation can nest arbitrarily; the core has nothing legitimate at this depth.
        const int MaxDepth = 8;

        if (type is null || depth > MaxDepth)
        {
            return false;
        }

        switch (type)
        {
            case IArrayTypeSymbol array:
                return CarriesFloatingPoint(array.ElementType, depth + 1);
            case IPointerTypeSymbol pointer:
                return CarriesFloatingPoint(pointer.PointedAtType, depth + 1);
        }

        if (type.SpecialType is SpecialType.System_Single
                             or SpecialType.System_Double
                             or SpecialType.System_Decimal)
        {
            return true;
        }

        if (MetadataName(type) is { } name && OpaqueFloatingPointTypes.Contains(name))
        {
            return true;
        }

        if (type is not INamedTypeSymbol named)
        {
            return false;
        }

        foreach (ITypeSymbol argument in named.TypeArguments)
        {
            if (CarriesFloatingPoint(argument, depth + 1))
            {
                return true;
            }
        }

        if (named.TypeKind != TypeKind.Struct || named.IsRefLikeType)
        {
            return false;
        }

        foreach (IFieldSymbol field in named.GetMembers().OfType<IFieldSymbol>())
        {
            if (!field.IsStatic && CarriesFloatingPoint(field.Type, depth + 1))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>True for a symbol that has source this analyser is allowed to point at.</summary>
    internal static bool IsReportableSource(ISymbol symbol) =>
        !symbol.IsImplicitlyDeclared
        && symbol.Locations.Length > 0
        && symbol.Locations[0].IsInSource;

    /// <summary>
    /// Peels the implicit conversions Roslyn inserts around a <c>foreach</c> collection and around
    /// an extension-method receiver, so the check sees the type that was actually written.
    /// </summary>
    internal static IOperation Unwrap(IOperation operation)
    {
        IOperation current = operation;
        while (current is IConversionOperation conversion && conversion.IsImplicit)
        {
            current = conversion.Operand;
        }

        return current;
    }

    /// <summary>
    /// <c>Namespace.Name`Arity</c> for a constructed or unconstructed generic, and
    /// <c>Namespace.Name</c> otherwise — the form the banned-type sets below are written in.
    /// </summary>
    internal static string? MetadataName(ITypeSymbol? type)
    {
        if (type is not INamedTypeSymbol named)
        {
            return null;
        }

        INamedTypeSymbol definition = named.OriginalDefinition;
        INamespaceSymbol? ns = definition.ContainingNamespace;

        return ns is null || ns.IsGlobalNamespace
            ? definition.MetadataName
            : $"{ns.ToDisplayString()}.{definition.MetadataName}";
    }

    /// <summary>The fully-qualified name of whatever a member reference resolved to.</summary>
    internal static string? ContainingTypeName(ISymbol? member) =>
        member?.ContainingType is { } type ? type.OriginalDefinition.ToDisplayString() : null;
}
