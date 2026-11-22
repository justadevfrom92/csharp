# Playground — run the projects in your browser

**Live: https://justadevfrom92.github.io/csharp/**

A Blazor WebAssembly app that runs the **real console projects** from this repo in the browser tab. Pick a project,
press **Run**, and you see exactly what `dotnet run` prints. Nothing runs on a server: GitHub Pages only hands out
files, and .NET itself (compiled to WebAssembly) comes along with them.

```bash
cd Playground
dotnet run
```

Then open http://localhost:5281.

```bash
cd Playground.Tests
dotnet test
```

## What's in it

| Project | Runs as |
|---|---|
| 1.1 Loan & Date Calculator | `dotnet run -- --demo` (answers typed in for you) |
| 1.2 Pattern Matching, 2.1 Inventory, 2.2 Iterators, 4.2 DDD Banking, 5.2 Channel Pipeline | `dotnet run` |
| 9.1 Operators & Lambdas, 9.4 Behavioral, 9.5 Pipelines, 9.6 Dataflow, 9.7 Workflow, 9.8 Rx | `dotnet run` |

**Not in it**, because a browser can't do it: web apps (they need a server: 3.x, 5.4, 6.3–6.5, 7.x, 8.x, 9.9, 10.1),
real threads (5.1), databases (6.1, 6.2, 6.6, 7.7), a benchmark run (5.3), files read from the project folder
(9.2, 9.3), and the 4.1 consumer (it needs the packed NuGet package).

## How it works

| Piece | Job | File |
|---|---|---|
| `ProjectReference` to each console project | the real `.dll`s are shipped to the browser | `Playground/Playground.csproj` |
| `ProjectCatalog` | the list: number, title, `.dll` name, arguments | `Catalogs/` |
| `ProgramRunnerService` | sends `Console.Out` to a `StringWriter`, finds the program's `Main` by reflection, awaits it | `Services/` |
| `Home.razor` | the list on the left, Run button, output | `Pages/` |
| `.github/workflows/pages.yml` | `dotnet publish`, set `<base href="/csharp/">`, deploy to GitHub Pages | repo root |

## Things found by running it in a real browser

Every project was run in headless Chromium from the published site and compared with `dotnet run`:

- **Console input can't be faked**: `Console.SetIn` throws `PlatformNotSupportedException` in a browser, so 1.1 uses its `--demo` answers.
- **5.2 crashed on `Console.CancelKeyPress`** (there's no Ctrl+C in a browser). 5.2 now hooks it only when `!OperatingSystem.IsBrowser()`.
- **1.1's Japanese date came out wrong** (`1/15/2027` instead of `2027/01/15`): by default the browser gets culture data for a few
  languages only. `BlazorWebAssemblyLoadAllGlobalizationData` fixes it; 1.1's output then matched `dotnet run` (except the clock time).
- **Blocking on `async` freezes it**: top-level programs with `await` get a hidden entry point that calls `.GetAwaiter().GetResult()`.
  The browser has one thread, so the runner calls the real async method `<Main>$` and awaits it.
- **Slower**: .NET runs interpreted in WebAssembly. 2.1's "1,000 lookups in a List" took about 5 seconds; the numbers in the
  timing lines differ from `dotnet run`, the conclusions don't.
- **One thread**: 5.2's three consumers and 9.6's four parsers take turns instead of running at once.
- **Trimming**: `dotnet publish` removes code that looks unused. The programs are started by reflection, which it can't see,
  so each one is listed in `<TrimmerRootAssembly>`.

## Code snippets

**Capturing what a program prints**

```csharp
var output = new StringWriter();
Console.SetOut(output);
object? result = main.Invoke(null, [args]);
if (result is Task task)
{
    await task;
}
```

**Finding the real async `Main` of top-level statements**

```csharp
MethodInfo entryPoint = assembly.EntryPoint!;
MethodInfo main = entryPoint.DeclaringType!.GetMethod("<Main>$", BindingFlags.NonPublic | BindingFlags.Static) ?? entryPoint;
```

**Code that must skip something in a browser**

```csharp
if (!OperatingSystem.IsBrowser())
{
    Console.CancelKeyPress += ...;
}
```

## Why these types

| Choice | Why | Why not the alternative |
|---|---|---|
| Blazor **WebAssembly** | runs on GitHub Pages (static files) | Blazor Server or MVC need a running server |
| `ProjectReference` to the real projects | the playground runs the **same code** as `dotnet run` | copying code would drift |
| reflection to call `Main` | one runner for every project | a hand-written page per project |
| `record RunnableProjectModel` | fixed data | — |
| `ProgramRunnerService` as a singleton | one browser tab = one user; it holds the "already running" flag | — |
| `ValidateExecutableReferencesMatchSelfContained=false` | the SDK refuses a self-contained app referencing normal exes; here it's on purpose | turning every project into a library |

## Tests

| Behaviour | PositivePositive | PositiveNegative | NegativeNegative |
|---|---|---|---|
| running a program | output captured (2.2) | async `Main` awaited (9.7); `--demo` args passed (1.1) | unknown project → reported as "Unhandled FileNotFoundException", not thrown |
| the catalog | every entry has a `Main` | numbers unique and in roadmap order | no server, threaded or database project listed |
