## S4 K1-K5 — `ddr2133-powersave-turbo`

Recorded 2026-07-31 14:43 UTC, pinned to core 2 with its SMT sibling idle.
Divide against `baseline-ddr2133-powersave-turbo.md`, which was measured under the same configuration.

```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Core i5-10400 CPU 2.90GHz (Max: 0.80GHz), 1 CPU, 12 logical and 6 physical cores
.NET SDK 10.0.110
  [Host] : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3
  s4     : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3

Job=s4  Concurrent=True  Server=False  

```
| Method               | Mean     | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|--------------------- |---------:|----------:|----------:|------:|--------:|----------:|------------:|
| SpanUnchecked        | 1.033 ms | 0.0190 ms | 0.0159 ms |  1.00 |    0.02 |         - |          NA |
| SpanChecked          | 1.316 ms | 0.0254 ms | 0.0226 ms |  1.27 |    0.03 |         - |          NA |
| PointerUnchecked     | 1.025 ms | 0.0187 ms | 0.0165 ms |  0.99 |    0.02 |         - |          NA |
| PointerChecked       | 1.733 ms | 0.0247 ms | 0.0231 ms |  1.68 |    0.03 |         - |          NA |
| PointerCheckedWalked | 1.313 ms | 0.0150 ms | 0.0125 ms |  1.27 |    0.02 |         - |          NA |

```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Core i5-10400 CPU 2.90GHz (Max: 0.80GHz), 1 CPU, 12 logical and 6 physical cores
.NET SDK 10.0.110
  [Host] : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3
  s4     : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3

Job=s4  Concurrent=True  Server=False  

```
| Method        | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------- |----------:|----------:|----------:|------:|--------:|----------:|------------:|
| SoaScattered  | 28.785 μs | 0.5722 μs | 0.7237 μs |  1.00 |    0.03 |         - |          NA |
| SoaSorted     | 26.654 μs | 0.5035 μs | 0.5798 μs |  0.93 |    0.03 |         - |          NA |
| SoaSequential |  2.844 μs | 0.0547 μs | 0.1154 μs |  0.10 |    0.00 |         - |          NA |
| AosScattered  | 19.308 μs | 0.3693 μs | 0.3455 μs |  0.67 |    0.02 |         - |          NA |
| AosSorted     | 19.213 μs | 0.3824 μs | 0.4552 μs |  0.67 |    0.02 |         - |          NA |

```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Core i5-10400 CPU 2.90GHz (Max: 0.80GHz), 1 CPU, 12 logical and 6 physical cores
.NET SDK 10.0.110
  [Host] : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3
  s4     : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3

Job=s4  Concurrent=True  Server=False  

```
| Method          | MeanWakeIntervalTicks | Mean     | Error    | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|---------------- |---------------------- |---------:|---------:|---------:|------:|--------:|----------:|------------:|
| **Revolution**      | **256**                   | **36.37 ns** | **0.785 ns** | **2.289 ns** |  **1.00** |    **0.09** |         **-** |          **NA** |
| SequentialFloor | 256                   | 10.87 ns | 0.122 ns | 0.108 ns |  0.30 |    0.02 |         - |          NA |
|                 |                       |          |          |          |       |         |           |             |
| **Revolution**      | **1024**                  | **35.44 ns** | **0.701 ns** | **1.367 ns** |  **1.00** |    **0.05** |         **-** |          **NA** |
| SequentialFloor | 1024                  | 11.11 ns | 0.206 ns | 0.202 ns |  0.31 |    0.01 |         - |          NA |
|                 |                       |          |          |          |       |         |           |             |
| **Revolution**      | **4096**                  | **37.00 ns** | **0.740 ns** | **1.729 ns** |  **1.00** |    **0.06** |         **-** |          **NA** |
| SequentialFloor | 4096                  | 10.99 ns | 0.216 ns | 0.240 ns |  0.30 |    0.01 |         - |          NA |

