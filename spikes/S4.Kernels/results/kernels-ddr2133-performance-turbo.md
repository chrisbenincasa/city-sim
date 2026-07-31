## S4 K1-K5 — `ddr2133-performance-turbo`

Recorded 2026-07-31 14:54 UTC, pinned to core 2 with its SMT sibling idle.
Divide against `baseline-ddr2133-performance-turbo.md`, which was measured under the same configuration.

```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Core i5-10400 CPU 2.90GHz (Max: 0.80GHz), 1 CPU, 12 logical and 6 physical cores
.NET SDK 10.0.110
  [Host] : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3
  s4     : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3

Job=s4  Concurrent=True  Server=False  

```
| Method               | Mean       | Error    | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|--------------------- |-----------:|---------:|---------:|------:|--------:|----------:|------------:|
| SpanUnchecked        |   991.7 μs | 14.11 μs | 11.01 μs |  1.00 |    0.02 |       1 B |        1.00 |
| SpanChecked          | 1,277.3 μs | 24.37 μs | 20.35 μs |  1.29 |    0.02 |         - |        0.00 |
| PointerUnchecked     |   994.9 μs | 14.92 μs | 13.22 μs |  1.00 |    0.02 |         - |        0.00 |
| PointerChecked       | 1,655.4 μs | 31.33 μs | 30.77 μs |  1.67 |    0.04 |         - |        0.00 |
| PointerCheckedWalked | 1,257.4 μs | 12.75 μs | 10.65 μs |  1.27 |    0.02 |         - |        0.00 |

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
| SoaScattered  | 27.314 μs | 0.5200 μs | 0.4864 μs |  1.00 |    0.02 |         - |          NA |
| SoaSorted     | 25.753 μs | 0.2910 μs | 0.2430 μs |  0.94 |    0.02 |         - |          NA |
| SoaSequential |  2.814 μs | 0.0551 μs | 0.1007 μs |  0.10 |    0.00 |         - |          NA |
| AosScattered  | 19.167 μs | 0.3544 μs | 0.3315 μs |  0.70 |    0.02 |         - |          NA |
| AosSorted     | 18.935 μs | 0.2171 μs | 0.1924 μs |  0.69 |    0.01 |         - |          NA |

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
| **Revolution**      | **256**                   | **35.41 ns** | **0.667 ns** | **1.611 ns** |  **1.00** |    **0.06** |         **-** |          **NA** |
| SequentialFloor | 256                   | 10.82 ns | 0.062 ns | 0.055 ns |  0.31 |    0.01 |         - |          NA |
|                 |                       |          |          |          |       |         |           |             |
| **Revolution**      | **1024**                  | **34.38 ns** | **0.611 ns** | **0.969 ns** |  **1.00** |    **0.04** |         **-** |          **NA** |
| SequentialFloor | 1024                  | 10.78 ns | 0.090 ns | 0.080 ns |  0.31 |    0.01 |         - |          NA |
|                 |                       |          |          |          |       |         |           |             |
| **Revolution**      | **4096**                  | **35.99 ns** | **0.703 ns** | **1.765 ns** |  **1.00** |    **0.07** |         **-** |          **NA** |
| SequentialFloor | 4096                  | 10.83 ns | 0.103 ns | 0.091 ns |  0.30 |    0.01 |         - |          NA |

