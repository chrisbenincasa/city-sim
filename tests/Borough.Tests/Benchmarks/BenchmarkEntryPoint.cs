using BenchmarkDotNet.Running;

namespace Borough.Tests.Benchmarks;

/// <summary>
/// Runs the benchmarks. <c>dotnet test</c> does not come through here.
/// </summary>
/// <remarks>
/// <para>
/// <b>Benchmarks live in <c>Borough.Tests</c> because <c>05 §1</c> puts them there</b> — <em>xUnit
/// and BenchmarkDotNet, determinism, invariants, save/reload, allocation and tick-time
/// benchmarks</em> — and <c>docs/dev-environment.md</c> already anticipated the package. A separate
/// benchmark project would have been a seventh, against a document whose first claim is that the
/// project split is the architectural decision.
/// </para>
/// <para>
/// <b>The two run modes do not collide.</b> <c>dotnet test</c> loads this assembly through VSTest
/// and never calls <c>Main</c>; <c>dotnet run</c> calls <c>Main</c> and never discovers a test. So
/// the benchmarks cannot slow the suite down and the suite cannot accidentally run a benchmark —
/// which matters, because a BenchmarkDotNet job takes minutes and CI takes seconds.
/// </para>
/// <para>
/// <b>Benchmarks are never assertions.</b> Nothing here fails a build. A timing threshold in CI is a
/// test that goes red on a busy machine and is then disabled, which leaves neither a benchmark nor a
/// test. What these produce is a number for a human to put in a document.
/// </para>
/// <example>
/// <code>
/// dotnet run --project tests/Borough.Tests -c Release -- --filter '*InvariantCost*'
/// </code>
/// </example>
/// </remarks>
public static class BenchmarkEntryPoint
{
    /// <summary>Dispatches to BenchmarkDotNet's switcher, which owns the argument grammar.</summary>
    public static void Main(string[] arguments) =>
        BenchmarkSwitcher
            .FromAssembly(typeof(BenchmarkEntryPoint).Assembly)
            .Run(arguments);
}
