## S4 K1-K5 — `ddr2133-powersave-turbo`

Recorded 2026-07-31 15:08 UTC, pinned to core 2 with its SMT sibling idle.
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
| SpanUnchecked        | 1.025 ms | 0.0192 ms | 0.0331 ms |  1.00 |    0.04 |         - |          NA |
| SpanChecked          | 1.294 ms | 0.0129 ms | 0.0108 ms |  1.26 |    0.04 |         - |          NA |
| PointerUnchecked     | 1.016 ms | 0.0184 ms | 0.0270 ms |  0.99 |    0.04 |         - |          NA |
| PointerChecked       | 1.677 ms | 0.0140 ms | 0.0124 ms |  1.64 |    0.05 |         - |          NA |
| PointerCheckedWalked | 1.303 ms | 0.0158 ms | 0.0140 ms |  1.27 |    0.04 |         - |          NA |

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
| SoaScattered  | 28.145 μs | 0.3630 μs | 0.2834 μs |  1.00 |    0.01 |         - |          NA |
| SoaSorted     | 26.766 μs | 0.5302 μs | 0.5445 μs |  0.95 |    0.02 |         - |          NA |
| SoaSequential |  2.841 μs | 0.0560 μs | 0.0667 μs |  0.10 |    0.00 |         - |          NA |
| AosScattered  | 19.342 μs | 0.3741 μs | 0.5244 μs |  0.69 |    0.02 |         - |          NA |
| AosSorted     | 18.921 μs | 0.1947 μs | 0.1626 μs |  0.67 |    0.01 |         - |          NA |

```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Core i5-10400 CPU 2.90GHz (Max: 0.80GHz), 1 CPU, 12 logical and 6 physical cores
.NET SDK 10.0.110
  [Host] : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3
  s4     : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3

Job=s4  Concurrent=True  Server=False  

```
| Method      | chunkBytes | Mean     | Error    | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------ |----------- |---------:|---------:|---------:|------:|--------:|----------:|------------:|
| **SingleBlock** | **?**          | **14.30 ms** | **0.169 ms** | **0.141 ms** |  **1.00** |    **0.01** |         **-** |          **NA** |
| PerColumn   | ?          | 17.70 ms | 0.244 ms | 0.204 ms |  1.24 |    0.02 |         - |          NA |
|             |            |          |          |          |       |         |           |             |
| **Chunked**     | **65536**      | **19.48 ms** | **0.234 ms** | **0.208 ms** |     **?** |       **?** |         **-** |           **?** |
|             |            |          |          |          |       |         |           |             |
| **Chunked**     | **1048576**    | **19.49 ms** | **0.167 ms** | **0.148 ms** |     **?** |       **?** |         **-** |           **?** |
|             |            |          |          |          |       |         |           |             |
| **Chunked**     | **8388608**    | **14.71 ms** | **0.128 ms** | **0.107 ms** |     **?** |       **?** |         **-** |           **?** |
|             |            |          |          |          |       |         |           |             |
| **Chunked**     | **33554432**   | **14.72 ms** | **0.119 ms** | **0.105 ms** |     **?** |       **?** |         **-** |           **?** |

```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Core i5-10400 CPU 2.90GHz (Max: 0.80GHz), 1 CPU, 12 logical and 6 physical cores
.NET SDK 10.0.110
  [Host] : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3
  s4     : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3

Job=s4  Concurrent=True  Server=False  

```
| Method               | Mean      | Error    | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|--------------------- |----------:|---------:|---------:|------:|--------:|----------:|------------:|
| EntryInterleaved     |  58.56 μs | 1.156 μs | 1.135 μs |  1.00 |    0.03 |         - |          NA |
| KeysThenValues       |  55.07 μs | 0.717 μs | 0.598 μs |  0.94 |    0.02 |         - |          NA |
| KeysThenValuesVector |  35.66 μs | 0.642 μs | 0.570 μs |  0.61 |    0.01 |         - |          NA |
| DictionaryLookup     | 109.01 μs | 2.105 μs | 2.505 μs |  1.86 |    0.05 |         - |          NA |

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
| **Revolution**      | **256**                   | **35.38 ns** | **0.693 ns** | **1.015 ns** |  **1.00** |    **0.04** |         **-** |          **NA** |
| SequentialFloor | 256                   | 10.85 ns | 0.068 ns | 0.060 ns |  0.31 |    0.01 |         - |          NA |
|                 |                       |          |          |          |       |         |           |             |
| **Revolution**      | **1024**                  | **35.73 ns** | **0.709 ns** | **1.315 ns** |  **1.00** |    **0.05** |         **-** |          **NA** |
| SequentialFloor | 1024                  | 10.87 ns | 0.096 ns | 0.080 ns |  0.30 |    0.01 |         - |          NA |
|                 |                       |          |          |          |       |         |           |             |
| **Revolution**      | **4096**                  | **35.62 ns** | **0.608 ns** | **1.410 ns** |  **1.00** |    **0.05** |         **-** |          **NA** |
| SequentialFloor | 4096                  | 10.86 ns | 0.069 ns | 0.061 ns |  0.31 |    0.01 |         - |          NA |

