# C# & .NET Roadmap Projects

Every project has its own `README.md` with how to run it and **why** it was built that way.

**Why `List` vs `IEnumerable`, `record` vs `class` vs `interface`, `decimal` vs `double`, `checked`...?**
The general rules are in [WHY_THESE_TYPES.md](WHY_THESE_TYPES.md), and every project README ends with a
**"Why these types"** table covering each choice made in that project.

**Where does each file go?** A class's name ends in its folder's name (`Forms/` → `...Form`, `Models/` → `...Model`).
See [FOLDER_STRUCTURE.md](FOLDER_STRUCTURE.md).

**New to `.cshtml` files (`@model`, `@foreach`, `ViewBag`, layouts)?** Read [RAZOR_BASICS.md](RAZOR_BASICS.md) first.

```bash
dotnet build CSharpMasteryRoadmap.slnx
```

| # | Project | Type |
|---|---|---|
| 1.1 | [Loan & Date Calculator](Section1_CoreFundamentals/Project1.1_SimpleLoanAndDateCalculator) | Console |
| 2.1 | [Warehouse Inventory & Orders](Section2_CollectionsAndLinq/Project2.1_InventoryAnalytics) | Console |
| 3.1 | [MVC Product Catalog](Section3_AspNetCoreMvc/Project3.1_ProductCatalog) | MVC |
| 3.2 | [Sidebar View Component & ISO Date Binder](Section3_AspNetCoreMvc/Project3.2_SidebarAndDateBinder) | MVC |
| 3.3 | [File Upload Portal](Section3_AspNetCoreMvc/Project3.3_FileUploadPortal) | MVC |
| 4.1 | [Custom NuGet Package](Section4_OopDddAndTesting/Project4.1_CustomNuGetPackage) | Library + console |
| 4.2 | [DDD Banking + xUnit](Section4_OopDddAndTesting/Project4.2_DddBankingEngine) | Library + tests |
| 4.3 | [Playwright Browser Tests](Section4_OopDddAndTesting/Project4.3_PlaywrightBrowserTests) | Tests (for 3.1) |
| 5.1 | [Threaded File Downloader](Section5_ConcurrencyAsyncAndMemory/Project5.1_ThreadedFileDownloader) | Console |
| 5.2 | [Channel Log Pipeline](Section5_ConcurrencyAsyncAndMemory/Project5.2_ChannelLogPipeline) | Console |
| 6.1 | [EF Core Query Logging](Section6_DatabaseStateAndErrorHandling/Project6.1_EfCoreQueryLogging) | MVC + SQLite |
| 6.2 | [Transactional Checkout & Soft Deletes](Section6_DatabaseStateAndErrorHandling/Project6.2_TransactionalCheckout) | Console + SQLite |
| 6.3 | [Shared Cache & Settings](Section6_DatabaseStateAndErrorHandling/Project6.3_SharedCacheAndSettings) | Web API |
| 6.4 | [Session Shopping Cart](Section6_DatabaseStateAndErrorHandling/Project6.4_SessionShoppingCart) | MVC |
| 6.5 | [Error Handling & Crash Recovery](Section6_DatabaseStateAndErrorHandling/Project6.5_ErrorHandlingAndCrashRecovery) | MVC + Serilog |
| 7.1 | [Cookie Login & Roles](Section7_SecurityBackgroundTasksAndDeployment/Project7.1_CookieAuthAndRoles) | MVC |
| 7.2 | [XSS & CSRF](Section7_SecurityBackgroundTasksAndDeployment/Project7.2_FormSecurityXssCsrf) | MVC |
| 7.3 | [Environment-Aware Email](Section7_SecurityBackgroundTasksAndDeployment/Project7.3_EnvironmentAwareEmail) | Web API + background services |
| 7.4 | [Output Caching + Health Check + robots.txt](Section7_SecurityBackgroundTasksAndDeployment/Project7.4_OutputCachingCatalog) | MVC + tests |
| 7.5 | [Feature Flags](Section7_SecurityBackgroundTasksAndDeployment/Project7.5_FeatureFlags) | MVC |
| 7.6 | [GitHub Actions CI](Section7_SecurityBackgroundTasksAndDeployment/Project7.6_GitHubActionsCi) | Workflow file |
| 7.7 | [Idempotent Migration Scripts](Section7_SecurityBackgroundTasksAndDeployment/Project7.7_IdempotentMigrationScripts) | Console + SQL script |

`Section4.../Project4.1.../ConsumerApp` is left out of the solution on purpose: it needs the package
packed first (see its README).
