using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Order;

namespace S4.Kernels.Harness;

/// <summary>
/// The BenchmarkDotNet configuration for K1-K5. plans/0004 task 1 is explicit that K6 is *not* a
/// BenchmarkDotNet job — "BDN's warmup-and-discard model is exactly wrong for a kernel whose whole
/// subject is the tail" — so nothing here is expected to serve it.
///
/// Deliberately close to BDN's defaults. The kernels are large enough that pilot-and-warmup does
/// the right thing unaided, and a hand-tuned job configuration is a way to make a measurement
/// unreproducible by anyone reading only the report.
/// </summary>
internal sealed class S4Config : ManualConfig
{
    public S4Config()
    {
        AddJob(Job.Default
            .WithId("s4")
            .WithGcServer(false)
            .WithGcConcurrent(true));

        AddDiagnoser(MemoryDiagnoser.Default);
        AddLogger(ConsoleLogger.Default);
        AddExporter(MarkdownExporter.GitHub);
        AddColumnProvider(DefaultColumnProviders.Instance);
        WithOrderer(DefaultOrderer.Instance);
        WithOptions(ConfigOptions.DisableOptimizationsValidator);
        WithArtifactsPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "results", "bdn"));
    }
}
