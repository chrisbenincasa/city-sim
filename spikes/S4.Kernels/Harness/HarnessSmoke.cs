using BenchmarkDotNet.Attributes;

namespace S4.Kernels.Harness;

/// <summary>
/// Not a kernel, and it decides nothing. It exists so that task 1 can prove the BenchmarkDotNet
/// toolchain actually builds and runs under this repository's pinned SDK and MSBuild properties —
/// BDN generates and compiles a project of its own, and discovering that it cannot is worth doing
/// before a kernel is written rather than after.
///
/// Delete this when K1 lands.
/// </summary>
public class HarnessSmoke
{
    private readonly byte[] _source = new byte[4096];
    private readonly byte[] _destination = new byte[4096];

    [Benchmark]
    public void CopyFourKiB() => _source.AsSpan().CopyTo(_destination);
}
