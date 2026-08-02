## S4 K1-K5 — `ddr2133-performance-turbo`

Recorded 2026-08-02 23:20 UTC, pinned to core 2 with its SMT sibling idle.
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
| SpanUnchecked        |   977.2 μs | 10.15 μs |  8.48 μs |  1.00 |    0.01 |         - |          NA |
| SpanChecked          | 1,257.1 μs |  8.29 μs |  6.92 μs |  1.29 |    0.01 |         - |          NA |
| PointerUnchecked     |   979.7 μs | 13.73 μs | 11.47 μs |  1.00 |    0.01 |         - |          NA |
| PointerChecked       | 1,633.2 μs | 10.31 μs |  9.14 μs |  1.67 |    0.02 |         - |          NA |
| PointerCheckedWalked | 1,251.0 μs | 11.44 μs |  9.56 μs |  1.28 |    0.01 |         - |          NA |

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
| SoaScattered  | 27.620 μs | 0.4353 μs | 0.3858 μs |  1.00 |    0.02 |         - |          NA |
| SoaSorted     | 25.851 μs | 0.3399 μs | 0.2838 μs |  0.94 |    0.02 |         - |          NA |
| SoaSequential |  2.717 μs | 0.0495 μs | 0.0386 μs |  0.10 |    0.00 |         - |          NA |
| AosScattered  | 18.887 μs | 0.1938 μs | 0.1718 μs |  0.68 |    0.01 |         - |          NA |
| AosSorted     | 18.630 μs | 0.2396 μs | 0.2124 μs |  0.67 |    0.01 |         - |          NA |

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
| **SingleBlock** | **?**          | **13.90 ms** | **0.048 ms** | **0.040 ms** |  **1.00** |    **0.00** |         **-** |          **NA** |
| PerColumn   | ?          | 17.18 ms | 0.059 ms | 0.053 ms |  1.24 |    0.01 |         - |          NA |
|             |            |          |          |          |       |         |           |             |
| **Chunked**     | **65536**      | **19.06 ms** | **0.089 ms** | **0.079 ms** |     **?** |       **?** |         **-** |           **?** |
|             |            |          |          |          |       |         |           |             |
| **Chunked**     | **1048576**    | **18.97 ms** | **0.069 ms** | **0.058 ms** |     **?** |       **?** |         **-** |           **?** |
|             |            |          |          |          |       |         |           |             |
| **Chunked**     | **8388608**    | **14.16 ms** | **0.039 ms** | **0.031 ms** |     **?** |       **?** |         **-** |           **?** |
|             |            |          |          |          |       |         |           |             |
| **Chunked**     | **33554432**   | **14.29 ms** | **0.041 ms** | **0.034 ms** |     **?** |       **?** |         **-** |           **?** |

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
| EntryInterleaved     |  56.52 μs | 0.332 μs | 0.260 μs |  1.00 |    0.01 |         - |          NA |
| KeysThenValues       |  53.87 μs | 0.328 μs | 0.274 μs |  0.95 |    0.01 |         - |          NA |
| KeysThenValuesVector |  34.76 μs | 0.457 μs | 0.405 μs |  0.62 |    0.01 |         - |          NA |
| DictionaryLookup     | 100.77 μs | 0.965 μs | 0.855 μs |  1.78 |    0.02 |         - |          NA |

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
| **Revolution**      | **256**                   | **32.37 ns** | **0.490 ns** | **0.434 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| SequentialFloor | 256                   | 10.58 ns | 0.084 ns | 0.074 ns |  0.33 |    0.00 |         - |          NA |
|                 |                       |          |          |          |       |         |           |             |
| **Revolution**      | **1024**                  | **32.94 ns** | **0.619 ns** | **0.662 ns** |  **1.00** |    **0.03** |         **-** |          **NA** |
| SequentialFloor | 1024                  | 10.63 ns | 0.038 ns | 0.034 ns |  0.32 |    0.01 |         - |          NA |
|                 |                       |          |          |          |       |         |           |             |
| **Revolution**      | **4096**                  | **32.61 ns** | **0.336 ns** | **0.281 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| SequentialFloor | 4096                  | 10.61 ns | 0.055 ns | 0.049 ns |  0.33 |    0.00 |         - |          NA |

