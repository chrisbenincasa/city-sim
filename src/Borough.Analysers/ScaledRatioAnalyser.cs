using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Borough.Analysers;

/// <summary>
/// <c>BOR0207</c> — a ratio pre-scaled by a large constant and divided in 32 bits.
/// </summary>
/// <remarks>
/// <para>
/// <b>The idiom, and why it is always a ratio.</b> Integer division discards the fraction, so the
/// only way to keep two decimal places out of <c>part ÷ whole</c> is to scale the numerator first:
/// <c>part * 10_000 / whole</c>. The scale factor has no other purpose, which is what makes the
/// shape recognisable — and what makes it dangerous, because the quantities a ratio is taken over
/// are the ones the surrounding code cares about most.
/// </para>
/// <para>
/// <b>Why a Q16.16 quantity makes it overflow almost immediately.</b> A fixed-point value is 65,536
/// times its whole value, so a cost of 33 Ticks is already 2.16 million. Scaled by 10,000 that is
/// 2.16 × 10¹⁰ against an <c>int.MaxValue</c> of 2.15 × 10⁹ — a wrap at <b>3.2 whole units</b>. It
/// wraps <i>negative</i>, so the largest inputs are subtracted from a mean rather than dominating
/// it, and what survives is a plausible small number nobody thinks to query.
/// </para>
/// <para>
/// <b>Why this cannot be left to BOR0203.</b> That lint routes every division through
/// <c>IntegerMath</c>, and <c>FloorDiv</c>, <c>CeilDiv</c> and <c>RoundDiv</c> all have <c>int</c>
/// overloads. <c>IntegerMath.FloorDiv(cost * 10_000, total)</c> binds to the 32-bit one, so the
/// multiplication has already overflowed inside the argument while the call site reads as though
/// the widening had been handled. Following the existing lint leads directly here, which is the
/// argument for a second one rather than against it.
/// </para>
/// <para>
/// <b>What is not reported.</b> A multiplication whose result is already 64-bit — one operand a
/// <c>long</c>, or an explicit <c>(long)</c> cast — has the headroom and is the fix this reports in
/// order to prompt. Scales below <see cref="ScaleThreshold"/> are not reported either: the rule
/// wants the pre-scaled-ratio idiom, not every multiplication by a small factor. See the threshold's
/// own note for what that bound is derived from.
/// </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ScaledRatioAnalyser : DiagnosticAnalyzer
{
    /// <summary>
    /// The smallest scale factor this reports, and it is derived rather than chosen. At a scale of
    /// 1,000 the headroom is <c>int.MaxValue / 1_000</c> = 2,147,483, which in Q16.16 is
    /// <b>32.77 whole units</b> — so any fixed-point quantity above about 33 already overflows.
    /// Below 1,000 the idiom is no longer distinguishable from ordinary unit conversion, and the
    /// remaining headroom is wide enough that flagging it would be noise rather than a finding.
    /// </summary>
    private const long ScaleThreshold = 1_000;

    /// <summary>
    /// The rounding-stating helpers BOR0203 sends every division to. Each has an <c>int</c> overload,
    /// so each is a route into this defect that looks like compliance with the other rule.
    /// </summary>
    private static readonly ImmutableHashSet<string> DivisionHelpers =
        ImmutableHashSet.Create("FloorDiv", "CeilDiv", "RoundDiv", "Div");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Diagnostics.ScaledRatioIn32Bits);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterOperationAction(AnalyseDivision, OperationKind.Binary);
        context.RegisterOperationAction(AnalyseHelperCall, OperationKind.Invocation);
    }

    /// <summary>
    /// A raw <c>/</c>. Legal only inside <c>Borough.Core.Arithmetic</c>, which is where the helpers
    /// are built — but the substrate is deliberately <b>not</b> exempt from this rule, unlike
    /// BOR0203's. Nothing there needs to overflow: <c>Fixed.Mul</c> already widens with
    /// <c>(long)a * b</c>, so correct substrate code does not trip this and incorrect substrate code
    /// is exactly what nobody else can catch.
    /// </summary>
    private static void AnalyseDivision(OperationAnalysisContext context)
    {
        var binary = (IBinaryOperation)context.Operation;

        if (binary.OperatorKind != BinaryOperatorKind.Divide || binary.OperatorMethod is not null)
        {
            return;
        }

        Report(context, binary.LeftOperand);
    }

    private static void AnalyseHelperCall(OperationAnalysisContext context)
    {
        var invocation = (IInvocationOperation)context.Operation;

        if (!DivisionHelpers.Contains(invocation.TargetMethod.Name)
            || invocation.Arguments.Length < 1)
        {
            return;
        }

        string? declaring = CoreConventions.ContainingTypeName(invocation.TargetMethod);
        if (declaring is not "Borough.Core.Arithmetic.IntegerMath"
                      and not "Borough.Core.Arithmetic.Fixed")
        {
            return;
        }

        Report(context, invocation.Arguments[0].Value);
    }

    /// <summary>
    /// Reports if the numerator is — after the conversions and parentheses Roslyn wraps around it —
    /// a 32-bit multiplication by a constant at or above the threshold.
    /// </summary>
    private static void Report(OperationAnalysisContext context, IOperation numerator)
    {
        IOperation inner = Peel(numerator);

        if (inner is not IBinaryOperation
            {
                OperatorKind: BinaryOperatorKind.Multiply, OperatorMethod: null,
            } multiply)
        {
            return;
        }

        if (!IsThirtyTwoBit(multiply.Type))
        {
            return;
        }

        long? scale = ConstantOf(multiply.RightOperand) ?? ConstantOf(multiply.LeftOperand);
        if (scale is not { } factor || System.Math.Abs(factor) < ScaleThreshold)
        {
            return;
        }

        long headroom = int.MaxValue / System.Math.Abs(factor);

        context.ReportDiagnostic(Diagnostic.Create(
            Diagnostics.ScaledRatioIn32Bits,
            multiply.Syntax.GetLocation(),
            multiply.Syntax.ToString(),
            factor,
            multiply.Type!.ToDisplayString(),
            headroom.ToString(System.Globalization.CultureInfo.InvariantCulture),
            (headroom / 65_536.0).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)));
    }

    /// <summary>
    /// Strips parentheses and conversions. <b>An explicit widening cast is the fix and must stop the
    /// walk</b> — <c>(long)(a * 10_000)</c> still multiplies in 32 bits and is reported, but the
    /// multiply's own type is what decides that, so peeling the outer cast is safe and peeling into
    /// a narrowing one would not be.
    /// </summary>
    private static IOperation Peel(IOperation operation)
    {
        IOperation current = operation;

        while (true)
        {
            switch (current)
            {
                case IParenthesizedOperation parenthesised:
                    current = parenthesised.Operand;
                    continue;
                case IConversionOperation conversion:
                    current = conversion.Operand;
                    continue;
                default:
                    return current;
            }
        }
    }

    private static long? ConstantOf(IOperation operand) =>
        Peel(operand).ConstantValue switch
        {
            { HasValue: true, Value: int value } => value,
            { HasValue: true, Value: long value } => value,
            { HasValue: true, Value: uint value } => value,
            { HasValue: true, Value: short value } => value,
            _ => null,
        };

    private static bool IsThirtyTwoBit(ITypeSymbol? type) =>
        type?.SpecialType is SpecialType.System_Int32 or SpecialType.System_UInt32;
}
