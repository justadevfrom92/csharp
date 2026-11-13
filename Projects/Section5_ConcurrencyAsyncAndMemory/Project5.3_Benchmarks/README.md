# Project 5.3 — Benchmarking with BenchmarkDotNet

| Folder | What |
|---|---|
| `Benchmarks/` | The code being measured (`Parsers/`, `Builders/`) and the benchmarks (`Benchmarks/`) |
| `Benchmarks.Tests/` | Tests proving each fast version gives **exactly** the same answers as the slow one |

## Run

Benchmarks **must** run in Release — a Debug build isn't optimised, so its numbers mean nothing.

```bash
dotnet run -c Release --project Benchmarks -- --filter "*"
```

Faster, less precise (good for a first look):

```bash
dotnet run -c Release --project Benchmarks -- --filter "*" --job short
```

Only one benchmark class:

```bash
dotnet run -c Release --project Benchmarks -- --filter "*Lookup*"
```

```bash
dotnet test Benchmarks.Tests
```

Results are printed and saved (Markdown, HTML, CSV) in `BenchmarkDotNet.Artifacts/results/` (ignored by git).

## Why BenchmarkDotNet and not `Stopwatch`

Project 5.1/5.2 time code with `Stopwatch`. That's fine for a rough idea, but it's easy to fool yourself:
the first run is slow (the JIT compiles the code), the garbage collector can pause in the middle, and one run
says nothing about how much results jump around. BenchmarkDotNet:
- runs a **warm-up** first, then many measured runs, and does the statistics;
- runs each benchmark in a **separate process**, so one can't slow down another;
- with `[MemoryDiagnoser]`, reports **memory allocated** per call;
- shows a **Ratio** against a `[Benchmark(Baseline = true)]` method, so comparisons are easy to read.

## The attributes used

| Attribute | Meaning |
|---|---|
| `[Benchmark]` | Measure this method |
| `[Benchmark(Baseline = true)]` | The method the others are compared to (Ratio = 1.00) |
| `[MemoryDiagnoser]` | Also report allocated memory and garbage collections |
| `[Params(10, 1000)]` | Run everything once per value, to see how cost grows with size |
| `[GlobalSetup]` | Runs once before measuring (building test data isn't counted) |

## Results (`--job short`, this dev container)

**Reading the columns:** *Mean* = average time per call (1 ns = a billionth of a second, 1 μs = 1,000 ns).
*Ratio* = time compared to the baseline. *Allocated* = memory created per call. *Gen0/Gen1* = garbage
collections per 1,000 calls. *Error* is large here because `--job short` does few runs — use the full run
for real decisions.

**Parsing the number out of a log line**

| Method | Mean | Ratio | Allocated |
|---|---|---|---|
| `string.Split` | 93.9 ns | 1.00 | 248 B |
| `ReadOnlySpan<char>` | 19.8 ns | **0.21** | **0 B** |

**Joining values into "a,b,c"**

| Method | 10 values | 1,000 values | Allocated (1,000) |
|---|---|---|---|
| `+=` in a loop | 243 ns | 567,170 ns | 8,821,680 B |
| `StringBuilder` | 214 ns | 9,524 ns | 50,976 B |
| `string.Join` | 65 ns | **5,839 ns** | **17,800 B** |

`+=` is fine for 10 values but explodes for 1,000: every `+=` copies the whole string so far, so the work grows
with the *square* of the count. It allocated ~8.8 MB to build one line.

**"Does this collection contain X?"** (searching for the last item)

| Method | 100 items | 100,000 items |
|---|---|---|
| `List.Contains` | 225 ns | **287,429 ns** |
| `HashSet.Contains` | 8.0 ns | 6.2 ns |
| `Dictionary.ContainsKey` | 7.2 ns | 8.6 ns |

The List checks items one by one (O(n)), so 1,000× more items ≈ 1,000× slower. HashSet and Dictionary jump
straight to the item (O(1)): the same few nanoseconds at any size. This is the measured version of the
Dictionary-vs-List argument in Project 2.1.

## Tests (every behaviour has all three kinds)

| Behaviour | PositivePositive | PositiveNegative | NegativeNegative |
|---|---|---|---|
| Parse duration (Split **and** Span) | valid lines → same number from both | well-formed line with `-5` ms → both refuse | `null`, empty, too few words, `twelve` instead of a number → both reject |
| Build CSV line (all three versions) | `a,b,c` and 1,000 values → identical output | empty list → `""` | `null` → all reject |

**Why test benchmark code at all:** a "faster" version that gives a different answer isn't faster, it's wrong.
The tests make sure every benchmark compares versions that really do the same job.

## Code snippets

**Run benchmarks (always in Release)**, from `Benchmarks/Program.cs`

```csharp
BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
```

```bash
dotnet run -c Release -- --filter "*Lookup*"
```

**A benchmark class**, from `Benchmarks/Benchmarks/LookupBenchmark.cs`

```csharp
[MemoryDiagnoser] // also measure memory allocated
public class LookupBenchmark
{
    [Params(100, 100_000)] // run everything once per size
    public int ItemCount { get; set; }

    [GlobalSetup] // runs once before measuring; not counted
    public void FillCollections()
    {
        list = Enumerable.Range(0, ItemCount).Select(number => "SKU-" + number).ToList();
        hashSet = new HashSet<string>(list);
        dictionary = list.ToDictionary(code => code, code => code.Length);
        lastCode = "SKU-" + (ItemCount - 1);
    }

    [Benchmark(Baseline = true)] // other results are shown relative to this one
    public bool ListContains()
    {
        return list.Contains(lastCode);
    }

    [Benchmark]
    public bool HashSetContains()
    {
        return hashSet.Contains(lastCode);
    }
}
```

**The code being measured: three ways to join strings**, from `Benchmarks/Builders/CsvLineBuilder.cs`

```csharp
line += (i == 0 ? "" : ",") + values[i]; // a brand-new string every time round the loop

builder.Append(values[i]);               // StringBuilder: adds to one growing buffer

return string.Join(',', values);         // works out the final length first, copies once
```
