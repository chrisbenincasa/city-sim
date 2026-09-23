using System.Numerics;
using BenchmarkDotNet.Attributes;
using Borough.Appearance;

namespace Borough.Tests.Benchmarks;

/// <summary>
/// How long one generated Building shell takes to build on a worker thread (procedural-buildings
/// REPORT §3, measurement 1). Arrays are reused, as a worker's would be.
/// </summary>
/// <example>
/// <code>
/// dotnet run --project tests/Borough.Tests -c Release -- --filter '*ShellBuilder*'
/// </code>
/// </example>
[MemoryDiagnoser]
public class ShellBuilderBenchmarks
{
    private static readonly Dictionary<string, WingShape[]> Buildings = new()
    {
        ["house"] = [new WingShape(8f, 10f, 2, 0, RoofForm.Gable, 11)],
        ["terrace"] = [new WingShape(6f, 12f, 3, 0, RoofForm.Parapet, 12)],
        ["courtyard"] =
        [
            new WingShape(30f, 10f, 5, 0, RoofForm.Flat, 13),
            new WingShape(30f, 10f, 5, -1, RoofForm.Flat, 13),
            new WingShape(10f, 10f, 5, -1, RoofForm.Flat, 13),
            new WingShape(10f, 10f, 5, -1, RoofForm.Flat, 13),
        ],
        ["tower"] =
        [
            new WingShape(30f, 30f, 2, 0, RoofForm.Flat, 14),
            new WingShape(20f, 20f, 20, -1, RoofForm.Parapet, 14),
        ],
    };

    private readonly ShellMesh _mesh = new();
    private WingShape[] _wings = [];
    private ShellMesh[] _workers = [];

    [Params("house", "terrace", "courtyard", "tower")]
    public string Building { get; set; } = "house";

    [GlobalSetup]
    public void Setup()
    {
        _wings = Buildings[Building];
        _workers = [.. Enumerable.Range(0, Environment.ProcessorCount).Select(_ => new ShellMesh())];
        Generate();
        Console.WriteLine($"// {Building}: {_mesh.VertexCount} vertices, {_mesh.IndexCount / 3} triangles");
    }

    [Benchmark(Baseline = true)]
    public int Generate()
    {
        _mesh.Clear();
        foreach (WingShape wing in _wings)
        {
            ShellBuilder.Append(_mesh, wing, Frame.At(new Vector3(100f, 0f, 200f), 0.5f));
        }

        return _mesh.VertexCount;
    }

    /// <summary>A thousand of the same Building across every core, one reused mesh per worker.</summary>
    [Benchmark(OperationsPerInvoke = 1000)]
    public void GenerateThousandInParallel()
    {
        Parallel.For(0, _workers.Length, worker =>
        {
            ShellMesh mesh = _workers[worker];
            for (int i = worker; i < 1000; i += _workers.Length)
            {
                mesh.Clear();
                foreach (WingShape wing in _wings)
                {
                    ShellBuilder.Append(mesh, wing, Frame.At(new Vector3(i, 0f, 200f), 0.5f));
                }
            }
        });
    }
}
