# Project 3.2 — Recent Activity Sidebar & ISO Date Binder

> New to `.cshtml` files (`@model`, `@foreach`, layouts)? Read [RAZOR_BASICS.md](../../RAZOR_BASICS.md) first.

```bash
dotnet run
```
Open `/`, add some activity, watch the sidebar update. Try `/Activity/Day?day=2026-11-05`
and a bad one like `/Activity/Day?day=05/11/2026`.

## What it covers

- `RecentActivitySidebarViewComponent` — used in the layout as `<vc:recent-activity-sidebar count="5" />`.
  It loads its own data with `InvokeAsync`.
- `CustomIsoDateFormatModelBinder` — an `IModelBinder` that only accepts `yyyy-MM-dd`.
  Applied to one parameter with `[ModelBinder(typeof(CustomIsoDateFormatModelBinder))]`.

## Why these choices

**Why a ViewComponent instead of a controller "child action".**
The sidebar is on *every* page. Old MVC used `Html.Action(...)` to call a second controller
action from inside a view; ASP.NET Core removed that because it ran the whole controller
pipeline (filters, model binding, routing) again just to draw a box.
A view component is lighter: one class, one method, one view. And the big win: no controller
has to load "recent activity" data and stuff it into `ViewBag` for every page. If we did that,
every action would need the same extra line, and forgetting it would break the sidebar.

**Why a custom binder for dates.**
The built-in binder reads dates using the server's culture, so `05/11/2026` could mean
May 11th or November 5th depending on where the server runs. The custom binder only accepts
`yyyy-MM-dd`, which can never be misread, and gives a clear error message for anything else.

**Why the binder is put on the parameter and not registered globally.** Only this one action
needs it. Applying it with an attribute keeps the rest of the app's binding normal.

## Why these types

General rules: [WHY_THESE_TYPES.md](../../WHY_THESE_TYPES.md). Choices made in this project:

| Choice | Why | Why not the alternative |
|---|---|---|
| `record ActivityEntry(DateOnly, TimeOnly, string)` | An activity is a fact — it never changes after it's logged. One line. | A `class` would add a lot of code and allow edits we don't want. |
| `DateOnly` + `TimeOnly` instead of one `DateTime` | "Entries for a day" becomes simple `entry.Date == day`; the URL takes just a date. | With `DateTime` every comparison needs `.Date` and a time-zone decision. |
| `static class ActivityLog` + static constructor | One shared log; the static constructor adds sample entries once. | Same trade-off as 3.1's `ProductStore`. |
| `List<ActivityEntry>` returns (copies) | Copied inside the lock, then sorted/filtered. | Lazy `IEnumerable` would escape the lock (see 3.1). |
| `class CustomIsoDateFormatModelBinder : IModelBinder` | MVC asks for this interface; any class implementing it can bind a value. | — |
| `Task` return from `BindModelAsync` | The interface demands `Task`. We do no async work, so we return `Task.CompletedTask`. | — |
| `DateOnly? day` parameter | Missing or invalid → `null` → we fall back to today. | Non-nullable `DateOnly` would silently become `0001-01-01`. |
| `Task<IViewComponentResult> InvokeAsync` | The usual view-component shape; ready if the data ever comes from a database. | Data is in memory, so a plain `Invoke` would also work. `Task.FromResult` is used because nothing is awaited. |
| `DailyActivityViewModel` (`Day`, `DateErrorMessage`, `Entries`) | Everything the day page needs in one typed object. | `ViewBag.Day` needed a cast in the view (`(DateOnly)ViewBag.Day`) and typos only fail at runtime. |
