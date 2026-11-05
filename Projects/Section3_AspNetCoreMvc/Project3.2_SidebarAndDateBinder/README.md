# Project 3.2 — Recent Activity Sidebar & ISO Date Binder

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
