# C# & .NET 10 Mastery Roadmap

A hands-on learning repo: **50 small, runnable projects** that walk through C# and .NET 10 from the basics
(variables, pattern matching, LINQ) to ASP.NET Core, EF Core, security, background work, real-time apps and
design patterns, and on into SaaS. The code is kept **deliberately simple**, every choice is explained, and most projects
have tests that prove what the README says: **713 xUnit tests in 32 test projects, plus 13 Playwright
browser tests**.

Everything lives in [`Projects/`](Projects). Start with [`Projects/README.md`](Projects/README.md), the full index.

**Run the projects in your browser:** https://justadevfrom92.github.io/csharp/ — the [Playground](Projects/Playground) is the
real console projects compiled to WebAssembly: pick one, press Run, see what `dotnet run` prints. Published by
[`.github/workflows/pages.yml`](https://github.com/justadevfrom92/csharp/blob/main/.github/workflows/pages.yml) on every push to `main`.

## What makes this repo different

| Feature | Where |
|---|---|
| **Every project explains "why"**: why `decimal` not `double`, `record` not `class`, `IEnumerable` not `List`... | a **Why these types** table at the end of each project README, plus [WHY_THESE_TYPES.md](Projects/WHY_THESE_TYPES.md) |
| **Variations**: the same code written other ways (other names, other APIs, common mistakes) side by side, with the differences proved | each project's `Variations/` folder and **Variations** README section |
| **Tests in three kinds** for every behaviour: PositivePositive (works), PositiveNegative (edge case), NegativeNegative (bad input) | each `*.Tests` project and a **Tests** table in its README |
| **Copy-paste snippets** | a **Code snippets** section in every README, and [cheat sheets](Projects/CheatSheets/README.md) (one per section, plus the `dotnet` CLI) |
| **Things found by running it**: surprises the tests or a real run uncovered, written down | "Things the tests showed" / "A bug the tests found" sections |
| **One naming rule for folders**: a class's name ends in its folder's name (`Models/` → `ProductModel`) | [FOLDER_STRUCTURE.md](Projects/FOLDER_STRUCTURE.md) |
| **Design pattern index**: where Strategy, Mediator, Decorator, Rx... appear | [DESIGN_PATTERNS.md](Projects/DESIGN_PATTERNS.md) |

## The sections

| # | Section | Projects | Covers |
|---|---|---|---|
| 1 | Core Fundamentals | 1.1–1.4 | types, `decimal` money math, dates, null handling, parsing, overflow, switch expressions and pattern matching, tuples and deconstruction, records with `with`, sealed hierarchies, C# 14 extension blocks |
| 2 | Collections & LINQ | 2.1–2.2 | `List`/`Dictionary`/`HashSet`, LINQ, deferred execution, iterators with `yield`, your own `IEnumerable`/`IEnumerator` |
| 3 | ASP.NET Core MVC | 3.1–3.3 | controllers, views, routing, model binding, validation, view components, custom model binders, safe file uploads |
| 4 | OOP, DDD & Testing | 4.1–4.3 | building a NuGet package, domain-driven design (value objects, aggregates, events), xUnit, Playwright browser tests |
| 5 | Concurrency, Async & Memory | 5.1–5.4 | threads, `async`/`await`, locks, channels, BenchmarkDotNet, async streams (`IAsyncEnumerable`) |
| 6 | Database, State & Error Handling | 6.1–6.6 | EF Core (logging, loading, transactions, soft deletes, raw SQL, bulk updates, concurrency), options, caching, sessions, error handling |
| 7 | Security, Background Tasks & Deployment | 7.1–7.9 | cookie auth and roles, XSS/CSRF, background services, output caching, health checks, feature flags, GitHub Actions, migration scripts and stored procedures, WebSockets, Redis |
| 8 | Beyond the Roadmap | 8.1–8.9 | minimal APIs + OpenAPI, Identity + JWT, `HttpClientFactory` + resilience, rate limiting, `dotnet publish` modes, Razor Pages, Blazor, SignalR, gRPC |
| 9 | Design Patterns & Architecture | 9.1–9.9 | operators and lambdas, JSON/YAML config, fluent builders and DSLs, behavioural patterns, pipelines, TPL Dataflow, workflow engine, Rx, service registration |
| 10 | SaaS | 10.1 | multi-tenancy: tenant per request, EF Core global query filters, tenant stamping, shared vs per-tenant databases |

The full list with links is in [`Projects/README.md`](Projects/README.md).

## Repository layout

```
.
├── README.md                      ← this file
├── .github/workflows/ci.yml       ← CI: build + all tests + browser tests + NuGet package check
├── .github/workflows/pages.yml    ← publishes the Playground to GitHub Pages
├── cli.txt                        ← dotnet CLI command reference
├── migration.txt                  ← EF Core migrations command reference
├── diff.txt                       ← Laravel ↔ ASP.NET Core 10 mapping
└── Projects/
    ├── README.md                  ← index of every project
    ├── CSharpMasteryRoadmap.slnx  ← one solution with every project
    ├── Directory.Build.props      ← shared settings: net10.0, nullable, implicit usings
    ├── global.json                ← .NET SDK version
    ├── dotnet-tools.json          ← local tools (dotnet-ef)
    ├── CheatSheets/               ← one snippet page per section + dotnet CLI
    ├── Playground/                ← Blazor WebAssembly app that runs the console projects in the browser
    ├── WHY_THESE_TYPES.md  FOLDER_STRUCTURE.md  DESIGN_PATTERNS.md  RAZOR_BASICS.md
    └── SectionN_Name/
        └── ProjectN.M_Name/
            ├── README.md          ← run it, what it shows, snippets, variations, why these types, tests
            ├── AppName/           ← the app (with a Variations/ folder)
            └── AppName.Tests/     ← xUnit tests (most projects)
```

## Run it

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```bash
cd Projects
```

```bash
dotnet build CSharpMasteryRoadmap.slnx
```

```bash
dotnet test CSharpMasteryRoadmap.slnx
```

Run one project. Each README gives its own command and port:

```bash
cd Projects/Section9_DesignPatternsAndArchitecture/Project9.6_Dataflow/DataflowImport
dotnet run
```

Some parts need extras:

| Needs | For |
|---|---|
| a Redis server (`localhost:6379`) | the 4 real-Redis tests in 7.9; they **skip** when Redis isn't running |
| Chromium for Playwright | 4.3 browser tests (see its README) |
| SQL Server | only to run 7.7 against a real database; generating its migration script needs no database |
| `dotnet tool restore` | the local `dotnet-ef` tool used by the EF Core projects |

## CI

[`.github/workflows/ci.yml`](https://github.com/justadevfrom92/csharp/blob/main/.github/workflows/ci.yml) runs on every push to `main` that touches `Projects/`:

1. **build-and-unit-tests**: restore, Release build, then every `*.Tests` project.
2. **e2e-tests**: Playwright browser tests against the 3.1 MVC app.
3. **nuget-package**: packs the 4.1 library and runs a consumer app against the `.nupkg`.

A newer push cancels an older run still in progress, so "cancelled" runs in the history aren't failures.
The workflow is explained line by line in [7.6's README](Projects/Section7_SecurityBackgroundTasksAndDeployment/Project7.6_GitHubActionsCi/README.md).

## House rules for the code

- **Keep it simple**: no extra layers, no features nobody asked for.
- **Braces on their own line** for every method: `{` goes on the next line.
- **One class per file**, simple names.
- **A class's name ends in its folder's name**: `Services/` → `...Service`, `Variations/` → `...Variation`.
- **Every behaviour is tested three ways**: PositivePositive, PositiveNegative, NegativeNegative.
- **Every claim in a README is checked** by a test or a real run; surprises are written down.
- **Explain "why"** for every choice, including the choice of type.

---

## Prompt to recreate this repo

Paste this into an AI coding agent (for example Claude Code) in an empty Git repository to build the same thing.
Results will differ in detail, but the structure, rules and topics will match.

```text
Build a C# / .NET 10 learning repository: many small, runnable projects that teach C# and .NET from basics to
architecture. Put ALL code and docs inside a "Projects" folder. The only files outside it are the root README.md
and .github/workflows/ci.yml.

GLOBAL SETUP
- Projects/Directory.Build.props: net10.0, ImplicitUsings enable, Nullable enable.
- Projects/global.json pinning the .NET 10 SDK (rollForward latestFeature).
- Projects/dotnet-tools.json with dotnet-ef.
- One solution, Projects/CSharpMasteryRoadmap.slnx, with every project in solution folders by section/project.
- Test stack: xUnit 2.x, Microsoft.NET.Test.Sdk, Microsoft.AspNetCore.Mvc.Testing (WebApplicationFactory) for web
  apps, bUnit for Blazor, Playwright for browser tests. EF Core with SQLite unless a topic needs SQL Server.

CODE RULES (follow strictly)
- Keep it stupidly simple. Short, plain code. No over-architecture, no extra layers, no features not asked for.
  No MVVM, no Docker.
- Every method, property accessor and type: the opening { on its own line.
- One class per file. Simple, descriptive names.
- Folder naming rule: a class's name ends in its folder's name (Models/ -> ProductModel, Services/ -> ...Service,
  Stores/ -> ...Store, DbContexts/ -> ...DbContext, Endpoints/ -> ...Endpoints, Generators/ -> ...Generator, etc.).
  Abstract base classes are named Base... . Document the rule in Projects/FOLDER_STRUCTURE.md.
- Every project has a Variations/ folder: other ways to write the same code (other names, other APIs, common
  mistakes), each class ending in "Variation". Exceptions: Razor Pages use Pages/Variations/*PageModel, Blazor uses
  Components/Variations/*Component.
- Comments explain WHY, briefly.

TESTING RULES
- Every behaviour gets three tests: PositivePositive (normal success), PositiveNegative (valid but edge case or
  surprising), NegativeNegative (invalid input / failure). Name them Behaviour_PositivePositive_Description etc.
- Variations get a VariationsTest.cs proving how they differ from the original.
- Run every claim before writing it down. If running shows something unexpected, fix the claim and record the
  finding in the README ("Things the tests showed").
- Tests that need an external server (Redis) skip when it isn't available.

EVERY PROJECT README HAS
- Title, what it builds, how to run it and its tests (bash blocks, one command per block).
- What it covers (table of what is in which file).
- Things found by running it.
- "## Code snippets": the key lines to copy.
- "## Variations": tables comparing the original with each variation.
- "## Why these types": a table of every type/design choice, why, and why not the alternative.
- "## Tests": a table of each behaviour with its PositivePositive / PositiveNegative / NegativeNegative cases.

PROJECTS (Projects/SectionN_Name/ProjectN.M_Name/)
Section 1 Core Fundamentals: 1.1 loan & date calculator (decimal money, dates, null handling, parsing, overflow);
  1.2 pattern matching (switch expressions, relational/type patterns); 1.3 tuples & deconstruction (named tuple
  returns, nullable tuples, switch on a tuple, swap, discards, Deconstruct on a class, dictionary/LINQ tuples;
  variations: out, Tuple<>, unnamed tuple, record, anonymous type; gotchas: JSON gives {} or Item1, == ignores names); 1.4 records, sealed hierarchies & extensions (p1 with { Name = ... },
  shallow copy of lists, records with lists not equal, abstract record + sealed kinds, switch needs _ (CS8509; future
  "closed" doesn't compile yet), C# 14 extension(...) blocks with methods/properties/static members vs classic this-methods,
  init checks with the field keyword and where with/constructors skip them).
Section 2 Collections & LINQ: 2.1 warehouse inventory (collections, LINQ, deferred execution);
  2.2 iterators (yield return/break, lazy file reader, endless sequence, own IEnumerable/IEnumerator).
Section 3 ASP.NET Core MVC: 3.1 product catalog (controllers, views, routing, binding, validation);
  3.2 sidebar view component + custom ISO date model binder; 3.3 safe file upload portal.
Section 4 OOP, DDD & Testing: 4.1 custom NuGet package + consumer app; 4.2 DDD banking engine (value objects,
  aggregates, domain events) with xUnit; 4.3 Playwright browser tests against 3.1.
Section 5 Concurrency, Async & Memory: 5.1 threaded file downloader (threads, locks, events, delegates);
  5.2 channel log pipeline; 5.3 BenchmarkDotNet benchmarks; 5.4 async streams (IAsyncEnumerable,
  [EnumeratorCancellation], EF Core AsAsyncEnumerable, .NET 10 async LINQ, streamed endpoint vs buffered).
Section 6 Database, State & Error Handling: 6.1 EF Core query logging (loading styles); 6.2 transactional
  checkout + soft deletes; 6.3 shared cache & options; 6.4 session shopping cart; 6.5 error handling & crash
  recovery (Serilog, IExceptionHandler); 6.6 EF Core advanced queries (FromSql, ExecuteUpdate/Delete, compiled
  queries, concurrency tokens).
Section 7 Security, Background Tasks & Deployment: 7.1 cookie auth & roles; 7.2 XSS & CSRF; 7.3 environment-aware
  email with background services and a queue; 7.4 output caching + health check + robots.txt; 7.5 feature flags;
  7.6 GitHub Actions CI explained; 7.7 idempotent migration scripts + stored procedures (SQL Server, EXEC(N'...'));
  7.8 raw WebSockets; 7.9 Redis distributed cache.
Section 8 Beyond the Roadmap: 8.1 minimal APIs + OpenAPI (+ entity vs DTO variation, over-posting); 8.2 Identity +
  JWT + policies; 8.3 HttpClientFactory + resilience; 8.4 rate limiting; 8.5 dotnet publish modes (trimming, AOT);
  8.6 Razor Pages; 8.7 Blazor (bUnit); 8.8 SignalR; 8.9 gRPC.
Section 9 Design Patterns & Architecture: 9.1 operator overloading & lambdas (generic delegates: Comparison,
  Converter, custom generic delegate; expression trees); 9.2 JSON & YAML configuration; 9.3 fluent builder & DSL;
  9.4 strategy, command, state, mediator, observer; 9.5 pipes & filters, chain of responsibility, decorator,
  interceptor (DispatchProxy); 9.6 TPL Dataflow; 9.7 workflow engine (retries, compensation, resume);
  9.8 Reactive Extensions; 9.9 service registration: builder.Services.RegisterAllExistingJobs() (assembly
  scanning), every registration style, plus reflection variations (GetProperties, SetValue, private methods,
  plugin loading) and a reflection-cost benchmark in 5.3.
Section 10 SaaS: 10.1 multi-tenancy (tenant from X-Tenant header via middleware into a scoped TenantContext; shared
  database with TenantId; EF Core global query filter reading a DbContext property; SaveChanges stamps TenantId and
  blocks cross-tenant writes; 404 for other tenants' ids; variations: claim/subdomain/path lookup, hand-written Where
  and the forgotten-Where leak, filter capturing a local variable (first tenant baked in), database per tenant).
  Later SaaS projects: organizations and invites, plans and limits, Stripe webhooks, audit log, outbox, OpenTelemetry.
If a section gets overloaded, split it and add more sections (11, 12, ...), within reason.

SHARED DOCS
- Projects/README.md: index table of every project with links and type.
- Projects/WHY_THESE_TYPES.md: general rules (List vs IEnumerable, record vs class vs struct, decimal vs double...).
- Projects/DESIGN_PATTERNS.md: every pattern and where it appears.
- Projects/RAZOR_BASICS.md: intro to .cshtml.
- Projects/CheatSheets/: one snippet page per section plus DotnetCli.md.
- appsettings.Development.json and Properties/launchSettings.json for every web project (unique ports).

CI (.github/workflows/ci.yml)
- On push to main touching Projects/**, pull requests, and manual dispatch; concurrency with cancel-in-progress.
- Job 1: restore, Release build of the solution, run every *.Tests.csproj, upload results.
- Job 2: Playwright end-to-end tests (install Chromium) against 3.1.
- Job 3: pack the 4.1 library and run its consumer app against the .nupkg.

PLAYGROUND + GITHUB PAGES (.github/workflows/pages.yml)
- Projects/Playground: a Blazor WebAssembly app that ProjectReferences every console project that can run in a
  browser (no threads, database, server or disk files) and runs its real Program: Console.SetOut to a StringWriter,
  find Main by reflection (prefer the async "<Main>$" of top-level programs - blocking freezes the one browser
  thread), await it, show the output. Keep the programs from being trimmed (TrimmerRootAssembly), load all
  globalization data, ValidateExecutableReferencesMatchSelfContained=false. Run every program in headless Chromium
  and compare with dotnet run; fix what differs.
- Workflow: dotnet publish the playground, set <base href="/csharp/">, copy index.html to 404.html, add .nojekyll,
  upload-pages-artifact, deploy-pages.

WORKFLOW
- Commit and push to main. Build with zero warnings. Run all tests before every push and check that CI is green.
- Finally, add a root README.md explaining the repo, with this prompt at the bottom.
```
