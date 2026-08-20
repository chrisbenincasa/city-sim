[assembly: Xunit.TestFramework("Borough.Tests.TierTimingFramework", "Borough.Tests")]

namespace Borough.Tests;

using System.Collections.Generic;
using System.Reflection;
using Xunit.Abstractions;
using Xunit.Sdk;

/// <summary>
/// Times every test in the assembly, so <see cref="TierBudget"/> has something to check.
/// </summary>
/// <remarks>
/// <para>
/// <b>An assembly-level hook rather than a per-test attribute, and that is the whole point.</b> The
/// budget must apply to a test <em>nobody thought about</em> — a new one, landing untagged — so the
/// mechanism cannot be something an author has to remember. xUnit's test-framework extension point
/// is the only place in v2 that sees every test with no annotation at any site.
/// </para>
/// <para>
/// ⚠ <b>It observes and never decides.</b> This class records durations; <c>TierBudgetTests</c>
/// reads them and is the thing that goes red. Keeping the two apart is what lets the check be a
/// <em>test</em> — visible in the same run, reported by the same runner, and subject to the same
/// review — rather than a runner flag somebody has to know to pass. ***A check that lives outside
/// the suite is a check that runs when somebody remembers it.***
/// </para>
/// <para>
/// ⚠ <b>It cannot see a test it did not run.</b> A filtered run times only what passed the filter,
/// so the budget is checked over the tests that actually ran and says nothing about the rest — which
/// is correct and is worth stating, because the opposite reading (<em>the budget was green, so the
/// suite is fast</em>) is exactly the false comfort <c>plans/0032</c> records the corpus already
/// falling for once with the 9m38s figure.
/// </para>
/// </remarks>
public sealed class TierTimingFramework(IMessageSink messageSink) : XunitTestFramework(messageSink)
{
    /// <inheritdoc/>
    protected override ITestFrameworkExecutor CreateExecutor(AssemblyName assemblyName) =>
        new TimingExecutor(assemblyName, SourceInformationProvider, DiagnosticMessageSink);

    private sealed class TimingExecutor(
        AssemblyName assemblyName,
        ISourceInformationProvider sourceInformationProvider,
        IMessageSink diagnosticMessageSink)
        : XunitTestFrameworkExecutor(assemblyName, sourceInformationProvider, diagnosticMessageSink)
    {
        protected override async void RunTestCases(
            IEnumerable<IXunitTestCase> testCases,
            IMessageSink executionMessageSink,
            ITestFrameworkExecutionOptions executionOptions)
        {
            using var assemblyRunner = new XunitTestAssemblyRunner(
                TestAssembly,
                testCases,
                DiagnosticMessageSink,
                new TimingSink(executionMessageSink),
                executionOptions);

            await assemblyRunner.RunAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Passes every message through untouched and keeps the duration off the ones that carry it.
    /// </summary>
    /// <remarks>
    /// <b>A pass-through rather than a replacement</b>: the runner, the loggers and the trx writer
    /// all read this stream, so swallowing or reordering a message here would break reporting for a
    /// measurement's convenience.
    /// </remarks>
    private sealed class TimingSink(IMessageSink inner) : LongLivedMarshalByRefObject, IMessageSink
    {
        public bool OnMessage(IMessageSinkMessage message)
        {
            if (message is ITestFinished finished)
            {
                TierBudget.Record(
                    $"{finished.TestCase.TestMethod.TestClass.Class.Name}.{finished.TestCase.TestMethod.Method.Name}",
                    System.TimeSpan.FromSeconds((double)finished.ExecutionTime));
            }

            // Every window in the assembly has closed by now, so a file append here cannot be the
            // cause of a jump somewhere else. plans/0002 §B.
            if (message is ITestAssemblyFinished)
            {
                AllocationProbe.Flush();
            }

            return inner.OnMessage(message);
        }
    }
}
