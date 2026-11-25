# Design Patterns & Techniques — where each one is

Every pattern below has a project that is **about** it (Section 9), and most also appear in earlier projects
where they're used for real.

| Pattern / technique | One-line meaning | Main example | Also used in |
|---|---|---|---|
| **DSL** (domain-specific language) | A small language made for one job | [9.3](Section9_DesignPatternsAndArchitecture/Project9.3_FluentBuilderAndDsl) `pricing-rules.txt` + parser | `.proto` files (8.9), GitHub Actions YAML (7.6), LINQ and EF's `modelBuilder` (internal DSLs) |
| **JSON / YAML configuration** | Settings in files, layered, bound to classes | [9.2](Section9_DesignPatternsAndArchitecture/Project9.2_JsonYamlConfiguration) | `appsettings.json` in 6.3, 7.3 (`appsettings.UAT.json`), 7.5, 8.1–8.3; YAML in `.github/workflows/ci.yml` (7.6) |
| **Operator overloading** | `+`, `*`, `<` for your own types | [9.1](Section9_DesignPatternsAndArchitecture/Project9.1_OperatorsAndLambdas) `MoneyValueObject` | `record` gives `==`/`!=` automatically (4.2) |
| **Lambda expressions** | Small functions passed around as values | [9.1](Section9_DesignPatternsAndArchitecture/Project9.1_OperatorsAndLambdas) | everywhere: LINQ (2.1), `Func`/`Action`/`Predicate` (5.1), minimal APIs (8.1), `Configure(options => ...)` |
| **Pipes and filters** | Every step runs, each changing the data | [9.5](Section9_DesignPatternsAndArchitecture/Project9.5_PipelinePatterns) `TextPipeline` | LINQ chains (2.1), channel pipeline (5.2), Dataflow (9.6) |
| **Chain of responsibility** | One handler deals with it; the others pass it on | [9.5](Section9_DesignPatternsAndArchitecture/Project9.5_PipelinePatterns) approval handlers | ASP.NET Core middleware ending a request early: `ShortCircuit()` (7.4), rate limiter (8.4), `RequireChatUserNameMiddleware` (8.8) |
| **Decorator pipeline** | Layers wrapping the same interface | [9.5](Section9_DesignPatternsAndArchitecture/Project9.5_PipelinePatterns) logging → validation → retry | email sender chain (7.3), `HttpClient` resilience handler (8.3) |
| **Dataflow architecture** | A graph of blocks, data flowing between them in parallel | [9.6](Section9_DesignPatternsAndArchitecture/Project9.6_Dataflow) | producer/consumer channel (5.2) |
| **Workflow engine** | Long process of steps with retries, undo and resume | [9.7](Section9_DesignPatternsAndArchitecture/Project9.7_WorkflowEngine) | transaction rollback (6.2) is the single-database version |
| **Fluent builder** | Build an object with chained calls | [9.3](Section9_DesignPatternsAndArchitecture/Project9.3_FluentBuilderAndDsl) `OrderBuilder` | used everywhere: `WebApplication.CreateBuilder`, `AddHttpClient(...).AddResilienceHandler(...)` (8.3), `AddAuthorizationBuilder()` (8.2) |
| **Mediator** | Send a request; something else finds the handler | [9.4](Section9_DesignPatternsAndArchitecture/Project9.4_BehavioralPatterns) `IMediator` | — |
| **Strategy** | Interchangeable ways to do one job | [9.4](Section9_DesignPatternsAndArchitecture/Project9.4_BehavioralPatterns) shipping strategies | `IEmailSender` implementations (7.3), keyed services (7.3) |
| **Observer / event-driven pipeline** | Tell every listener "this happened" | [9.4](Section9_DesignPatternsAndArchitecture/Project9.4_BehavioralPatterns) `OrderEventPublisher` | C# events (5.1), domain events + dispatcher (4.2), SignalR (8.8), gRPC streaming `StockChanged` (8.9), Rx (9.8) |
| **Command** | An action as an object (undo, redo, queue) | [9.4](Section9_DesignPatternsAndArchitecture/Project9.4_BehavioralPatterns) editor with undo/redo | stack-based undo of orders (2.1), queued emails (7.3) |
| **State** | Each state is a class deciding what's allowed | [9.4](Section9_DesignPatternsAndArchitecture/Project9.4_BehavioralPatterns) order states | closed account rules (4.2) |
| **Interceptor / AOP** | Add logging/timing/security around every call without touching the code | [9.5](Section9_DesignPatternsAndArchitecture/Project9.5_PipelinePatterns) `DispatchProxy` | EF Core `DbCommandInterceptor` (6.1), MVC action filter (7.5), endpoint filter (8.1), middleware (7.2, 7.4) |
| **Reactive Extensions (Rx / `IObservable`)** | LINQ over events arriving over time | [9.8](Section9_DesignPatternsAndArchitecture/Project9.8_ReactiveExtensions) | — |

## Other patterns already in the repo

| Pattern | Where |
|---|---|
| Repository | `IBankAccountRepository` (4.2) |
| Value object, aggregate, domain events (DDD) | 4.2; an aggregate stored with EF Core (private collection, shadow keys) in 6.7 |
| Dependency injection: every `builder.Services` style, assembly scanning (`RegisterAllExistingJobs`) | [9.9](Section9_DesignPatternsAndArchitecture/Project9.9_ServiceRegistration); also 6.3, 7.3, 9.4 |
| Options pattern (`IOptions`, `IOptionsMonitor`) | 6.3, 7.5 |
| Cache-aside | 6.3, 7.9 |
| Producer / consumer | 5.2, 7.3 |
| Retry with backoff, timeout | 8.3, 9.5, 9.7 |
| Unit of work / transaction | 6.2 |
| Soft delete + global query filter | 6.2 |
| Optimistic concurrency | 6.6 |
| Post/Redirect/Get | 3.1, 6.4, 8.6 |
| Saga / compensation | 9.7 |
