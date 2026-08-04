## S4 K1-K5 — `apple-m4-pro`

Recorded 2026-08-04 19:26 UTC. **Not pinned** — macOS does not allow a thread to be
pinned to a core at all, so this is measured with the scheduler placing threads and the
absolutes must be read as a shape rather than as points. The ratios are the comparable part.
Divide against `baseline-apple-m4-pro.md`, measured in the same sitting.

```

BenchmarkDotNet v0.15.8, macOS Tahoe 26.5.2 (25F84) [Darwin 25.5.0]
Apple M4 Pro, 1 CPU, 14 logical and 14 physical cores
.NET SDK 10.0.102
  [Host] : .NET 10.0.2 (10.0.2, 10.0.225.61305), Arm64 RyuJIT armv8.0-a
  s4     : .NET 10.0.2 (10.0.2, 10.0.225.61305), Arm64 RyuJIT armv8.0-a

Job=s4  Concurrent=True  Server=False  

```
| Method               | Mean     | Error   | StdDev  | Ratio | Allocated | Alloc Ratio |
|--------------------- |---------:|--------:|--------:|------:|----------:|------------:|
| SpanUnchecked        | 377.7 μs | 0.22 μs | 0.19 μs |  1.00 |         - |          NA |
| SpanChecked          | 486.9 μs | 4.61 μs | 4.31 μs |  1.29 |         - |          NA |
| PointerUnchecked     | 417.9 μs | 0.24 μs | 0.22 μs |  1.11 |         - |          NA |
| PointerChecked       | 859.3 μs | 0.71 μs | 0.59 μs |  2.28 |         - |          NA |
| PointerCheckedWalked | 503.6 μs | 2.20 μs | 2.06 μs |  1.33 |         - |          NA |

```

BenchmarkDotNet v0.15.8, macOS Tahoe 26.5.2 (25F84) [Darwin 25.5.0]
Apple M4 Pro, 1 CPU, 14 logical and 14 physical cores
.NET SDK 10.0.102
  [Host] : .NET 10.0.2 (10.0.2, 10.0.225.61305), Arm64 RyuJIT armv8.0-a
  s4     : .NET 10.0.2 (10.0.2, 10.0.225.61305), Arm64 RyuJIT armv8.0-a

Job=s4  Concurrent=True  Server=False  

```
| Method        | Mean     | Error     | StdDev    | Ratio | Allocated | Alloc Ratio |
|-------------- |---------:|----------:|----------:|------:|----------:|------------:|
| SoaScattered  | 3.692 μs | 0.0139 μs | 0.0130 μs |  1.00 |         - |          NA |
| SoaSorted     | 3.429 μs | 0.0051 μs | 0.0043 μs |  0.93 |         - |          NA |
| SoaSequential | 1.124 μs | 0.0012 μs | 0.0011 μs |  0.30 |         - |          NA |
| AosScattered  | 5.203 μs | 0.0209 μs | 0.0195 μs |  1.41 |         - |          NA |
| AosSorted     | 5.136 μs | 0.0156 μs | 0.0146 μs |  1.39 |         - |          NA |

```

BenchmarkDotNet v0.15.8, macOS Tahoe 26.5.2 (25F84) [Darwin 25.5.0]
Apple M4 Pro, 1 CPU, 14 logical and 14 physical cores
.NET SDK 10.0.102
  [Host] : .NET 10.0.2 (10.0.2, 10.0.225.61305), Arm64 RyuJIT armv8.0-a
  s4     : .NET 10.0.2 (10.0.2, 10.0.225.61305), Arm64 RyuJIT armv8.0-a

Job=s4  Concurrent=True  Server=False  

```
| Method      | chunkBytes | Mean     | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------ |----------- |---------:|----------:|----------:|------:|--------:|----------:|------------:|
| **SingleBlock** | **?**          | **3.077 ms** | **0.0096 ms** | **0.0090 ms** |  **1.00** |    **0.00** |         **-** |          **NA** |
| PerColumn   | ?          | 3.105 ms | 0.0023 ms | 0.0020 ms |  1.01 |    0.00 |         - |          NA |
|             |            |          |           |           |       |         |           |             |
| **Chunked**     | **65536**      | **3.868 ms** | **0.0032 ms** | **0.0030 ms** |     **?** |       **?** |         **-** |           **?** |
|             |            |          |           |           |       |         |           |             |
| **Chunked**     | **1048576**    | **3.119 ms** | **0.0023 ms** | **0.0019 ms** |     **?** |       **?** |         **-** |           **?** |
|             |            |          |           |           |       |         |           |             |
| **Chunked**     | **8388608**    | **3.079 ms** | **0.0026 ms** | **0.0023 ms** |     **?** |       **?** |         **-** |           **?** |
|             |            |          |           |           |       |         |           |             |
| **Chunked**     | **33554432**   | **3.078 ms** | **0.0030 ms** | **0.0025 ms** |     **?** |       **?** |         **-** |           **?** |

```

BenchmarkDotNet v0.15.8, macOS Tahoe 26.5.2 (25F84) [Darwin 25.5.0]
Apple M4 Pro, 1 CPU, 14 logical and 14 physical cores
.NET SDK 10.0.102
  [Host] : .NET 10.0.2 (10.0.2, 10.0.225.61305), Arm64 RyuJIT armv8.0-a
  s4     : .NET 10.0.2 (10.0.2, 10.0.225.61305), Arm64 RyuJIT armv8.0-a

Job=s4  Concurrent=True  Server=False  

```
| Method               | Mean     | Error    | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|--------------------- |---------:|---------:|---------:|------:|--------:|----------:|------------:|
| EntryInterleaved     | 22.12 μs | 0.177 μs | 0.157 μs |  1.00 |    0.01 |         - |          NA |
| KeysThenValues       | 22.10 μs | 0.124 μs | 0.104 μs |  1.00 |    0.01 |         - |          NA |
| KeysThenValuesVector | 12.87 μs | 0.248 μs | 0.243 μs |  0.58 |    0.01 |         - |          NA |
| DictionaryLookup     | 33.72 μs | 0.645 μs | 0.603 μs |  1.52 |    0.03 |         - |          NA |

```

BenchmarkDotNet v0.15.8, macOS Tahoe 26.5.2 (25F84) [Darwin 25.5.0]
Apple M4 Pro, 1 CPU, 14 logical and 14 physical cores
.NET SDK 10.0.102
  [Host] : .NET 10.0.2 (10.0.2, 10.0.225.61305), Arm64 RyuJIT armv8.0-a
  s4     : .NET 10.0.2 (10.0.2, 10.0.225.61305), Arm64 RyuJIT armv8.0-a

Job=s4  Concurrent=True  Server=False  

```
| Method          | MeanWakeIntervalTicks | Mean     | Error     | StdDev    | Ratio | Allocated | Alloc Ratio |
|---------------- |---------------------- |---------:|----------:|----------:|------:|----------:|------------:|
| **Revolution**      | **256**                   | **8.624 ns** | **0.0326 ns** | **0.0305 ns** |  **1.00** |         **-** |          **NA** |
| SequentialFloor | 256                   | 2.367 ns | 0.0016 ns | 0.0014 ns |  0.27 |         - |          NA |
|                 |                       |          |           |           |       |           |             |
| **Revolution**      | **1024**                  | **8.425 ns** | **0.0479 ns** | **0.0448 ns** |  **1.00** |         **-** |          **NA** |
| SequentialFloor | 1024                  | 2.366 ns | 0.0019 ns | 0.0017 ns |  0.28 |         - |          NA |
|                 |                       |          |           |           |       |           |             |
| **Revolution**      | **4096**                  | **8.458 ns** | **0.0461 ns** | **0.0432 ns** |  **1.00** |         **-** |          **NA** |
| SequentialFloor | 4096                  | 2.361 ns | 0.0030 ns | 0.0026 ns |  0.28 |         - |          NA |

