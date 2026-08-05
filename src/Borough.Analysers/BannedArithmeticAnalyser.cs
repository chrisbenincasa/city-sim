using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Borough.Analysers;

/// <summary>
/// The rest of <c>05 §4</c>'s banned-construct table that is arithmetic: <c>Math.*</c>, raw
/// <c>/</c>, and shifting by a computed count.
/// </summary>
/// <remarks>
/// <para>
/// <b>The one exemption is <c>Borough.Core.Arithmetic</c>.</b> That namespace is where the
/// stated-rounding and range-checked helpers are implemented, and they are implemented out of the
/// very operators this bans. The exemption is a namespace rather than a per-site suppression so
/// that it is visible in one place and cannot spread by copy — and it is matched <b>exactly</b>, so
/// a child namespace does not inherit it. Inventing <c>Borough.Core.Arithmetic.Whatever</c> is
/// precisely the copy this was meant to prevent.
/// </para>
/// <para>
/// <b>A constant shift count is legal.</b> <c>05 §4</c> bans the *non-constant* shift specifically:
/// the hazard is that C# masks the count against the operand width, so <c>x &lt;&lt; 32</c> silently
/// means <c>x &lt;&lt; 0</c>. A count written as a literal or a <c>const</c> is visible in the source
/// and cannot vary, and <c>Randomness.Mix</c> is built out of exactly those.
/// </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class BannedArithmeticAnalyser : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            Diagnostics.MathMember,
            Diagnostics.RawDivision,
            Diagnostics.NonConstantShift);

    private static readonly ImmutableHashSet<string> MathTypes =
        ImmutableHashSet.Create("System.Math", "System.MathF");

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterOperationAction(AnalyseMemberUse,
            OperationKind.Invocation, OperationKind.PropertyReference, OperationKind.FieldReference);

        context.RegisterOperationAction(AnalyseBinary, OperationKind.Binary);
        context.RegisterOperationAction(AnalyseCompoundAssignment, OperationKind.CompoundAssignment);
    }

    private static void AnalyseMemberUse(OperationAnalysisContext context)
    {
        ISymbol? member = context.Operation switch
        {
            IInvocationOperation invocation => invocation.TargetMethod,
            IPropertyReferenceOperation property => property.Property,
            IFieldReferenceOperation field => field.Field,
            _ => null,
        };

        if (member is null || !MathTypes.Contains(CoreConventions.ContainingTypeName(member) ?? ""))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            Diagnostics.MathMember,
            context.Operation.Syntax.GetLocation(),
            $"{member.ContainingType.Name}.{member.Name}"));
    }

    private static void AnalyseBinary(OperationAnalysisContext context)
    {
        var binary = (IBinaryOperation)context.Operation;

        // A user-defined operator is somebody else's decision, already made under these rules —
        // Ratio's `/` routes through Fixed.Div. Only the built-in operators are this rule's target.
        if (binary.OperatorMethod is not null || CoreConventions.IsArithmeticSubstrate(context.ContainingSymbol))
        {
            return;
        }

        switch (binary.OperatorKind)
        {
            case BinaryOperatorKind.Divide when IsBuiltInInteger(binary.Type):
                context.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.RawDivision,
                    binary.Syntax.GetLocation(),
                    binary.Type!.ToDisplayString()));
                break;

            case BinaryOperatorKind.LeftShift
              or BinaryOperatorKind.RightShift
              or BinaryOperatorKind.UnsignedRightShift
                when !binary.RightOperand.ConstantValue.HasValue:
                context.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.NonConstantShift, binary.Syntax.GetLocation()));
                break;

            default:
                break;
        }
    }

    /// <summary>
    /// <c>x /= 2</c> and <c>x &lt;&lt;= n</c> are the same two operators wearing an assignment, and
    /// they do not surface as <see cref="OperationKind.Binary"/>.
    /// </summary>
    private static void AnalyseCompoundAssignment(OperationAnalysisContext context)
    {
        var assignment = (ICompoundAssignmentOperation)context.Operation;

        if (assignment.OperatorMethod is not null
            || CoreConventions.IsArithmeticSubstrate(context.ContainingSymbol))
        {
            return;
        }

        switch (assignment.OperatorKind)
        {
            case BinaryOperatorKind.Divide when IsBuiltInInteger(assignment.Type):
                context.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.RawDivision,
                    assignment.Syntax.GetLocation(),
                    assignment.Type!.ToDisplayString()));
                break;

            case BinaryOperatorKind.LeftShift
              or BinaryOperatorKind.RightShift
              or BinaryOperatorKind.UnsignedRightShift
                when !assignment.Value.ConstantValue.HasValue:
                context.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.NonConstantShift, assignment.Syntax.GetLocation()));
                break;

            default:
                break;
        }
    }

    /// <summary>
    /// Unwraps <see cref="System.Nullable{T}"/> first. A lifted built-in operator — <c>int? / 2</c> —
    /// has a result type of <c>int?</c>, whose <c>SpecialType</c> is <c>None</c> and whose
    /// <c>OperatorMethod</c> is still null, so it slipped past both guards at once.
    /// </summary>
    private static bool IsBuiltInInteger(ITypeSymbol? type)
    {
        if (type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } lifted)
        {
            type = lifted.TypeArguments[0];
        }

        return IsBuiltInIntegerCore(type);
    }

    private static bool IsBuiltInIntegerCore(ITypeSymbol? type) =>
        type?.SpecialType is SpecialType.System_SByte or SpecialType.System_Byte
                          or SpecialType.System_Int16 or SpecialType.System_UInt16
                          or SpecialType.System_Int32 or SpecialType.System_UInt32
                          or SpecialType.System_Int64 or SpecialType.System_UInt64
                          or SpecialType.System_IntPtr or SpecialType.System_UIntPtr
                          or SpecialType.System_Char;
}
