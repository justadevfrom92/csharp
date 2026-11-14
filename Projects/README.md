# C# & .NET Roadmap Projects

Every project has its own `README.md` with how to run it and **why** it was built that way.

**Why `List` vs `IEnumerable`, `record` vs `class` vs `interface`, `decimal` vs `double`, `checked`...?**
The general rules are in [WHY_THESE_TYPES.md](WHY_THESE_TYPES.md), and every project README ends with a
**"Why these types"** table covering each choice made in that project.

**Where does each file go?** A class's name ends in its folder's name (`Forms/` → `...Form`, `Models/` → `...Model`).
See [FOLDER_STRUCTURE.md](FOLDER_STRUCTURE.md).

**Which design pattern is where?** Strategy, Mediator, Decorator, DSL, Rx and more: [DESIGN_PATTERNS.md](DESIGN_PATTERNS.md).

**Need code to copy?** The [cheat sheets](CheatSheets/README.md) have short snippets for every topic (one page per
section, plus the `dotnet` CLI), and every project README has a **Code snippets** section with the key lines from that project.

**New to `.cshtml` files (`@model`, `@foreach`, `ViewBag`, layouts)?** Read [RAZOR_BASICS.md](RAZOR_BASICS.md) first.

```bash
dotnet build CSharpMasteryRoadmap.slnx
```

| # | Project | Type |
|---|---|---|
| 1.1 | [Loan & Date Calculator](Section1_CoreFundamentals/Project1.1_SimpleLoanAndDateCalculator) | Console |
| 1.2 | [Pattern Matching](Section1_CoreFundamentals/Project1.2_PatternMatching) | Console + tests |
| 2.1 | [Warehouse Inventory & Orders](Section2_CollectionsAndLinq/Project2.1_InventoryAnalytics) | Console |
| 3.1 | [MVC Product Catalog](Section3_AspNetCoreMvc/Project3.1_ProductCatalog) | MVC |
| 3.2 | [Sidebar View Component & ISO Date Binder](Section3_AspNetCoreMvc/Project3.2_SidebarAndDateBinder) | MVC |
| 3.3 | [File Upload Portal](Section3_AspNetCoreMvc/Project3.3_FileUploadPortal) | MVC |
| 4.1 | [Custom NuGet Package](Section4_OopDddAndTesting/Project4.1_CustomNuGetPackage) | Library + console |
| 4.2 | [DDD Banking + xUnit](Section4_OopDddAndTesting/Project4.2_DddBankingEngine) | Library + tests |
| 4.3 | [Playwright Browser Tests](Section4_OopDddAndTesting/Project4.3_PlaywrightBrowserTests) | Tests (for 3.1) |
| 5.1 | [Threaded File Downloader](Section5_ConcurrencyAsyncAndMemory/Project5.1_ThreadedFileDownloader) | Console |
| 5.2 | [Channel Log Pipeline](Section5_ConcurrencyAsyncAndMemory/Project5.2_ChannelLogPipeline) | Console |
| 5.3 | [Benchmarks (BenchmarkDotNet)](Section5_ConcurrencyAsyncAndMemory/Project5.3_Benchmarks) | Console + tests |
| 6.1 | [EF Core Query Logging](Section6_DatabaseStateAndErrorHandling/Project6.1_EfCoreQueryLogging) | MVC + SQLite |
| 6.2 | [Transactional Checkout & Soft Deletes](Section6_DatabaseStateAndErrorHandling/Project6.2_TransactionalCheckout) | Console + SQLite |
| 6.3 | [Shared Cache & Settings](Section6_DatabaseStateAndErrorHandling/Project6.3_SharedCacheAndSettings) | Web API |
| 6.4 | [Session Shopping Cart](Section6_DatabaseStateAndErrorHandling/Project6.4_SessionShoppingCart) | MVC |
| 6.5 | [Error Handling & Crash Recovery](Section6_DatabaseStateAndErrorHandling/Project6.5_ErrorHandlingAndCrashRecovery) | MVC + Serilog |
| 6.6 | [EF Core Advanced Queries](Section6_DatabaseStateAndErrorHandling/Project6.6_EfCoreAdvancedQueries) | Console + SQLite + tests |
| 7.1 | [Cookie Login & Roles](Section7_SecurityBackgroundTasksAndDeployment/Project7.1_CookieAuthAndRoles) | MVC + tests |
| 7.2 | [XSS & CSRF](Section7_SecurityBackgroundTasksAndDeployment/Project7.2_FormSecurityXssCsrf) | MVC |
| 7.3 | [Environment-Aware Email](Section7_SecurityBackgroundTasksAndDeployment/Project7.3_EnvironmentAwareEmail) | Web API + background services |
| 7.4 | [Output Caching + Health Check + robots.txt](Section7_SecurityBackgroundTasksAndDeployment/Project7.4_OutputCachingCatalog) | MVC + tests |
| 7.5 | [Feature Flags](Section7_SecurityBackgroundTasksAndDeployment/Project7.5_FeatureFlags) | MVC |
| 7.6 | [GitHub Actions CI](Section7_SecurityBackgroundTasksAndDeployment/Project7.6_GitHubActionsCi) | Workflow file |
| 7.7 | [Idempotent Migration Scripts & Stored Procedures](Section7_SecurityBackgroundTasksAndDeployment/Project7.7_IdempotentMigrationScripts) | Console + SQL Server |
| 7.8 | [Raw WebSockets](Section7_SecurityBackgroundTasksAndDeployment/Project7.8_RawWebSockets) | Web + tests |
| 7.9 | [Redis Distributed Cache](Section7_SecurityBackgroundTasksAndDeployment/Project7.9_RedisDistributedCache) | Web API + tests |

`Section4.../Project4.1.../ConsumerApp` is left out of the solution on purpose: it needs the package
packed first (see its README).

## Section 8 — Beyond the Roadmap

Not in the original roadmap spec, but big parts of Microsoft's .NET 10 / ASP.NET Core documentation.

| # | Project | Type |
|---|---|---|
| 8.1 | [Minimal APIs + OpenAPI](Section8_BeyondTheRoadmap/Project8.1_MinimalApiOpenApi) | Web API + tests |
| 8.2 | [Identity + JWT + Policies](Section8_BeyondTheRoadmap/Project8.2_IdentityJwtPolicies) | Web API + tests |
| 8.3 | [HttpClientFactory + Resilience](Section8_BeyondTheRoadmap/Project8.3_HttpClientResilience) | Web API + tests |
| 8.4 | [Rate Limiting](Section8_BeyondTheRoadmap/Project8.4_RateLimiting) | Web API + tests |
| 8.5 | [`dotnet publish` (every mode)](Section8_BeyondTheRoadmap/Project8.5_DotnetPublish) | Web API + tests + publish script |
| 8.6 | [Razor Pages](Section8_BeyondTheRoadmap/Project8.6_RazorPages) | Razor Pages + tests |
| 8.7 | [Blazor](Section8_BeyondTheRoadmap/Project8.7_Blazor) | Blazor + bUnit tests |
| 8.8 | [SignalR](Section8_BeyondTheRoadmap/Project8.8_SignalR) | Real-time + tests |
| 8.9 | [gRPC](Section8_BeyondTheRoadmap/Project8.9_Grpc) | gRPC + tests |

## Section 9 — Design Patterns & Architecture

Patterns and techniques for structuring code. **Every one of them, and where else it appears in this repo:
[DESIGN_PATTERNS.md](DESIGN_PATTERNS.md).**

| # | Project | Type |
|---|---|---|
| 9.1 | [Operator Overloading & Lambda Expressions](Section9_DesignPatternsAndArchitecture/Project9.1_OperatorsAndLambdas) | Console + tests |
| 9.2 | [JSON & YAML Configuration](Section9_DesignPatternsAndArchitecture/Project9.2_JsonYamlConfiguration) | Console + tests |
| 9.3 | [Fluent Builder & DSL](Section9_DesignPatternsAndArchitecture/Project9.3_FluentBuilderAndDsl) | Console + tests |
| 9.4 | [Strategy, Command, State, Mediator, Observer](Section9_DesignPatternsAndArchitecture/Project9.4_BehavioralPatterns) | Console + tests |
| 9.5 | [Pipes & Filters, Chain of Responsibility, Decorator Pipeline, Interceptor/AOP](Section9_DesignPatternsAndArchitecture/Project9.5_PipelinePatterns) | Console + tests |
| 9.6 | [Dataflow Architecture (TPL Dataflow)](Section9_DesignPatternsAndArchitecture/Project9.6_Dataflow) | Console + tests |
| 9.7 | [Workflow Engine (retries, compensation, resume)](Section9_DesignPatternsAndArchitecture/Project9.7_WorkflowEngine) | Console + tests |
| 9.8 | [Reactive Extensions (Rx / IObservable)](Section9_DesignPatternsAndArchitecture/Project9.8_ReactiveExtensions) | Console + tests |
