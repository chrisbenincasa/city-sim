using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Borough.Analysers;

/// <summary>
/// <c>05 §4</c> lint 3, first half — a hash map may be <em>built</em> and <em>looked up</em>, and it
/// may not be <em>walked</em>.
/// </summary>
/// <remarks>
/// <para>
/// <b>That distinction is the whole rule and the diagnostic says so.</b> A lint that read "no
/// Dictionary in the core" would be worked around rather than obeyed, because a dictionary keyed by
/// a Ruleset name and read at lookup is a perfectly deterministic thing to own. What is not
/// deterministic is its iteration order: .NET randomises string hashing per process, so the order
/// differs between runs of the same binary on the same machine — and being per-*process* means it
/// will not reproduce for whoever is debugging it.
/// </para>
/// <para>
/// <b><c>SortedDictionary</c> and <c>SortedSet</c> are deliberately absent from the banned set.</b>
/// Their order comes from a comparer rather than from a hash, so walking one is reproducible. The
/// interfaces <em>are</em> banned, because an <c>IDictionary&lt;,&gt;</c> hides which of the two it
/// is holding. So are the <c>Frozen</c> collections, which is the trap worth naming: .NET 8+
/// guidance points at <c>FrozenDictionary</c> for exactly the read-mostly lookup table this rule
/// blesses, so it is what somebody obeying the rule reaches for — and then walks. The base chain is
/// walked too, so a subclass does not launder the ban.
/// </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class HashMapOrderAnalyser : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Diagnostics.HashMapEnumeration);

    private static readonly ImmutableHashSet<string> HashOrdered = ImmutableHashSet.Create(
        "System.Collections.Generic.Dictionary`2",
        "System.Collections.Generic.HashSet`1",
        "System.Collections.Generic.IDictionary`2",
        "System.Collections.Generic.IReadOnlyDictionary`2",
        "System.Collections.Generic.ISet`1",
        "System.Collections.Generic.IReadOnlySet`1",
        "System.Collections.Concurrent.ConcurrentDictionary`2",
        "System.Collections.Immutable.ImmutableDictionary`2",
        "System.Collections.Immutable.ImmutableHashSet`1",
        "System.Collections.Frozen.FrozenDictionary`2",
        "System.Collections.Frozen.FrozenSet`1",
        "System.Collections.Hashtable");

    /// <summary>
    /// Members that hand out an ordering. <c>Keys</c> and <c>Values</c> are the two that read as
    /// innocent; <c>GetEnumerator</c> is the one a hand-written loop reaches for.
    /// </summary>
    private static readonly ImmutableHashSet<string> OrderingMembers =
        ImmutableHashSet.Create("Keys", "Values", "GetEnumerator");

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterOperationAction(AnalyseLoop, OperationKind.Loop);
        context.RegisterOperationAction(AnalysePropertyReference, OperationKind.PropertyReference);
        context.RegisterOperationAction(AnalyseInvocation, OperationKind.Invocation);
    }

    private static void AnalyseLoop(OperationAnalysisContext context)
    {
        if (context.Operation is not IForEachLoopOperation loop)
        {
            return;
        }

        ITypeSymbol? collection = CoreConventions.Unwrap(loop.Collection).Type;
        if (IsHashOrdered(collection))
        {
            Report(context, loop.Collection.Syntax.GetLocation(), collection!);
        }
    }

    private static void AnalysePropertyReference(OperationAnalysisContext context)
    {
        var reference = (IPropertyReferenceOperation)context.Operation;
        if (OrderingMembers.Contains(reference.Property.Name)
            && IsHashOrdered(reference.Property.ContainingType))
        {
            Report(context, reference.Syntax.GetLocation(), reference.Property.ContainingType);
        }
    }

    /// <summary>
    /// <c>GetEnumerator()</c> called by hand, and every LINQ operator — which is the door a
    /// <c>foreach</c> ban would leave wide open, since <c>.Select(…)</c> walks just as hard.
    /// </summary>
    private static void AnalyseInvocation(OperationAnalysisContext context)
    {
        var invocation = (IInvocationOperation)context.Operation;
        IMethodSymbol target = invocation.TargetMethod;

        if (OrderingMembers.Contains(target.Name) && IsHashOrdered(target.ContainingType))
        {
            Report(context, invocation.Syntax.GetLocation(), target.ContainingType);
            return;
        }

        if (!target.IsExtensionMethod)
        {
            return;
        }

        IOperation? receiver = invocation.Instance
            ?? (invocation.Arguments.Length > 0 ? invocation.Arguments[0].Value : null);

        ITypeSymbol? receiverType = receiver is null ? null : CoreConventions.Unwrap(receiver).Type;
        if (IsHashOrdered(receiverType))
        {
            Report(context, invocation.Syntax.GetLocation(), receiverType!);
        }
    }

    /// <summary>
    /// Walks the base chain, so <c>sealed class Index : Dictionary&lt;int, int&gt;</c> does not
    /// launder the ban through a subclass.
    /// </summary>
    private static bool IsHashOrdered(ITypeSymbol? type)
    {
        for (ITypeSymbol? current = type; current is not null; current = current.BaseType)
        {
            if (CoreConventions.MetadataName(current) is { } name && HashOrdered.Contains(name))
            {
                return true;
            }
        }

        return false;
    }

    private static void Report(OperationAnalysisContext context, Location location, ITypeSymbol type) =>
        context.ReportDiagnostic(Diagnostic.Create(
            Diagnostics.HashMapEnumeration, location, type.OriginalDefinition.ToDisplayString()));
}
