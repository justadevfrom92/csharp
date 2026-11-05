# Mini-Project 3.2 — Dynamic Sidebar View Component & Custom ISO Date Model Binder

## Run

```bash
dotnet run      # http://localhost:5032
```

Try:
* `/activity?from=2026-11-01&to=2026-11-05&kind=OrderPlaced`: complex type bound from the query string
* `/activity?from=11/05/2026`: rejected with 400 and an explanatory message
* `/activity/day/2026-11-05`: route value bound by the globally registered binder
* `/activity/record`: POST a new event and watch the sidebar update on the next page

## Components

| Type | Purpose |
|---|---|
| `ViewComponents/RecentActivitySidebarViewComponent` | Loads the latest N activity entries itself (`InvokeAsync`) and renders `Default` or `Empty` view |
| `ModelBinding/CustomIsoDateFormatModelBinder` | `IModelBinder` that accepts only `yyyy-MM-dd` for `DateOnly`/`DateOnly?` |
| `ModelBinding/CustomIsoDateFormatModelBinderProvider` | Registers the binder globally (inserted at index 0 of `ModelBinderProviders`) |
| `Models/ActivityReportDateRangeQuery` | Complex query object; its date properties opt in explicitly via `[ModelBinder(typeof(CustomIsoDateFormatModelBinder), Name = "from")]` |
| `Services/InMemoryRecentActivityFeedService` | Singleton, async-shaped, bounded to 500 entries (prevents an unbounded memory leak) |

## Why a `ViewComponent` instead of a controller child action (the required write-up)

Classic ASP.NET MVC 5 rendered widgets with `@Html.Action("RecentActivity", "Sidebar")`. That
was a **child action**: a nested run of the full controller pipeline. ASP.NET Core removed child
actions, and the view component is the designed replacement. It is also the better design for these reasons:

1. **No controller pollution.** The sidebar appears on every page. Without a view component,
   *every* action in *every* controller would have to fetch activity entries and push them into
   `ViewBag`/a base view model, or inherit from a base controller that does. Here,
   `HomeController.Index` is a one-liner, and no controller references `IRecentActivityFeedService`
   except the one that owns the activity feature.
2. **Single responsibility and encapsulation.** Data access (`GetMostRecentEntriesAsync`), the
   empty-state decision (`Default` vs `Empty` view) and markup live together in one unit with
   one dependency injected through its constructor.
3. **Not reachable over HTTP.** A child action was still an action. Unless it was marked
   `[ChildActionOnly]` it could be requested directly by URL, which is an accidental public endpoint. A view
   component has no route, so it cannot be invoked from outside.
4. **Lighter execution.** A child action ran model binding, action filters, authorization filters and
   result filters for a fragment of HTML. A view component skips the filter pipeline entirely; it is
   just "call `InvokeAsync`, render a view".
5. **Async-first and testable.** `InvokeAsync` returns `Task<IViewComponentResult>`, so it can await I/O
   without blocking a thread. The class can be unit-tested by constructing it with a fake service.
6. **Strongly typed invocation.** `<vc:recent-activity-sidebar maximum-entry-count="5" />` is a tag
   helper with IntelliSense and compile-time parameter names, instead of magic strings.

*When a partial view is still better:* when the markup needs no data of its own (see the
header/footer partials in Project 3.1).

## Why a custom ISO date binder

* The built-in binder parses with the **request culture**, so `03/04/2026` is 4 March for an en-US visitor
  and 3 April for an en-GB visitor. A URL can be bookmarked, shared or cached by a proxy and then read by
  someone with a different culture, so the same URL must always mean the same date.
* `yyyy-MM-dd` (ISO-8601) is unambiguous, sorts lexically and is what `<input type="date">` submits.
* The binder **adds a model-state error** with a precise, actionable message ("Use the ISO format
  yyyy-MM-dd") instead of silently binding `null`. That error surfaces as HTTP 400.
* Two registration styles are shown on purpose. **Global** (provider at index 0, so it wins over
  the framework's simple-type binder) gives consistency everywhere. **Explicit `[ModelBinder]`** on
  `ActivityReportDateRangeQuery` makes the dependency visible where it is used and renames
  the query keys (`from`/`to`) independently of the C# property names.
* Naming: `CustomIsoDateFormatModelBinder` states *what* it binds (dates), *which rule*
  (ISO format) and *what it is* (a model binder). Compare a vague `DateBinder`.
