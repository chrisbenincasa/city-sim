```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Core i5-10400 CPU 2.90GHz (Max: 0.80GHz), 1 CPU, 12 logical and 6 physical cores
.NET SDK 10.0.112
  [Host] : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Toolchain=InProcessEmitToolchain  

```
| Method                     | Building  | Mean       | Error     | StdDev    | Median     | Ratio | RatioSD | Allocated | Alloc Ratio |
|--------------------------- |---------- |-----------:|----------:|----------:|-----------:|------:|--------:|----------:|------------:|
| **Generate**                   | **courtyard** | **137.789 μs** | **1.7652 μs** | **1.6512 μs** | **137.657 μs** |  **1.00** |    **0.02** |         **-** |          **NA** |
| GenerateThousandInParallel | courtyard |  22.422 μs | 1.2515 μs | 3.3187 μs |  20.799 μs |  0.16 |    0.02 |       5 B |          NA |
|                            |           |            |           |           |            |       |         |           |             |
| **Generate**                   | **house**     |   **7.756 μs** | **0.0468 μs** | **0.0391 μs** |   **7.760 μs** |  **1.00** |    **0.01** |         **-** |          **NA** |
| GenerateThousandInParallel | house     |   1.313 μs | 0.0261 μs | 0.0457 μs |   1.303 μs |  0.17 |    0.01 |       4 B |          NA |
|                            |           |            |           |           |            |       |         |           |             |
| **Generate**                   | **terrace**   |  **12.616 μs** | **0.2098 μs** | **0.1860 μs** |  **12.555 μs** |  **1.00** |    **0.02** |         **-** |          **NA** |
| GenerateThousandInParallel | terrace   |   4.839 μs | 0.0798 μs | 0.0919 μs |   4.818 μs |  0.38 |    0.01 |       4 B |          NA |
|                            |           |            |           |           |            |       |         |           |             |
| **Generate**                   | **tower**     | **196.376 μs** | **2.7358 μs** | **2.5591 μs** | **195.512 μs** |  **1.00** |    **0.02** |       **1 B** |        **1.00** |
| GenerateThousandInParallel | tower     |  34.050 μs | 1.7829 μs | 5.2009 μs |  31.574 μs |  0.17 |    0.03 |       5 B |        5.00 |
