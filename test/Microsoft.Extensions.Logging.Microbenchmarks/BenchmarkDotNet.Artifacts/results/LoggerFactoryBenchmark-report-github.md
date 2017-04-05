``` ini

BenchmarkDotNet=v0.10.3.0, OS=Microsoft Windows 6.3.9600
Processor=Intel(R) Xeon(R) CPU E5-1620 0 3.60GHz, ProcessorCount=8
Frequency=3507176 Hz, Resolution=285.1297 ns, Timer=TSC
dotnet cli version=2.0.0-preview1-005418
  [Host]     : .NET Core 4.6.25009.03, 64bit RyuJIT
  Job-EMOQVB : .NET Core 4.6.25009.03, 64bit RyuJIT

RemoveOutliers=False  Runtime=Core  Server=True  
LaunchCount=3  RunStrategy=Throughput  TargetCount=10  
WarmupCount=5  

```
 |       Method |        Mean |     StdDev |       Op/s | Allocated |
 |------------- |------------ |----------- |----------- |---------- |
 | ElevenFilter | 827.1133 ns | 24.2051 ns | 1209024.18 |     196 B |
