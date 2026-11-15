# Project 3.2 — Recent Activity Sidebar & ISO Date Binder

> New to `.cshtml` files (`@model`, `@foreach`, `ViewBag`, layouts)? Read [RAZOR_BASICS.md](../../RAZOR_BASICS.md) first.

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

## Code snippets

**A view component: a piece of a page that loads its own data**, from `ViewComponents/RecentActivitySidebarViewComponent.cs`

```csharp
public class RecentActivitySidebarViewComponent : ViewComponent
{
    public Task<IViewComponentResult> InvokeAsync(int count)
    {
        List<ActivityEntryModel> newestEntries = ActivityLogStore.GetNewest(count);
        IViewComponentResult result = View(newestEntries); // Views/Shared/Components/RecentActivitySidebar/Default.cshtml
        return Task.FromResult(result);
    }
}
```

Used in `Views/Shared/_Layout.cshtml` (class name in kebab-case, without `ViewComponent`):

```cshtml
<vc:recent-activity-sidebar count="5" />
```

**A custom model binder: read a date in one exact format**, from `ModelBinders/CustomIsoDateFormatModelBinder.cs`

```csharp
public Task BindModelAsync(ModelBindingContext bindingContext)
{
    string? text = bindingContext.ValueProvider.GetValue(bindingContext.ModelName).FirstValue;
    if (string.IsNullOrWhiteSpace(text))
    {
        return Task.CompletedTask; // nothing sent: the action picks a default
    }
    if (DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly date))
    {
        bindingContext.Result = ModelBindingResult.Success(date);
    }
    else
    {
        bindingContext.ModelState.AddModelError(bindingContext.ModelName, $"'{text}' is not a date in yyyy-MM-dd format.");
    }
    return Task.CompletedTask;
}
```

**Using the binder on one parameter**, from `Controllers/ActivityController.cs`

```csharp
// GET /Activity/Day?day=2026-11-05
public IActionResult Day([ModelBinder(typeof(CustomIsoDateFormatModelBinder))] DateOnly? day)
{
    if (!ModelState.IsValid)
    {
        ViewBag.Error = ModelState["day"]?.Errors.FirstOrDefault()?.ErrorMessage;
    }
    DateOnly chosenDay = day ?? DateOnly.FromDateTime(DateTime.Today);
    return View(ActivityLogStore.GetForDay(chosenDay));
}
```

## Variations

Other ways to write the same code, in `Variations/` (controllers on `/variations/...`; results checked with `curl`).

**Reading `?day=...`** (`DateBindingVariation`, `/variations/dates/...`)

| `?day=` | `custom` (original binder, `yyyy-MM-dd` only) | `default` (built-in `DateOnly?` binding) | `manual` (`string` + `TryParseExact` in the action) |
|---|---|---|---|
| `2026-11-05` | 2026-11-05 | 2026-11-05 | 2026-11-05 |
| `11/05/2026` | ❌ "not a date in yyyy-MM-dd format" | **2026-11-05** (read as month/day) | ❌ same message |
| `05.11.2026` | ❌ | **2026-05-11** (read as 11 May!) | ❌ |
| `abc` | ❌ | ❌ "The value 'abc' is not valid." | ❌ |

The built-in binding reads query strings with InvariantCulture and **accepts many formats** — a British user
typing `11/05/2026` (11 May) silently gets 5 November. The custom binder and the manual parse refuse anything but
`yyyy-MM-dd`; the binder does it once for every action, the manual version repeats the code in each action.

**Showing the sidebar** (`SidebarVariation`, `/variations/sidebar/...`)

| Way | The difference |
|---|---|
| `<vc:recent-activity-sidebar count="5" />` (original, in the layout) | name and parameters checked at compile time |
| `@await Component.InvokeAsync("RecentActivitySidebar", new { count = 2 })` (`/invoke`) | same HTML; name is a **string**, parameters an anonymous object — typos only show when the page runs |
| `<partial name="..." model="Model" />` (`/partial`) | a partial can't load data: the **controller** had to fetch the list and pass it in |
| `return ViewComponent("RecentActivitySidebar", new { count })` from an action (`/only`) | just the sidebar's HTML, no layout — for refreshing that piece with JavaScript |

## Why these types

General rules: [WHY_THESE_TYPES.md](../../WHY_THESE_TYPES.md). Choices made in this project:

| Choice | Why | Why not the alternative |
|---|---|---|
| `record ActivityEntryModel(DateOnly, TimeOnly, string)` | An activity is a fact — it never changes after it's logged. One line. | A `class` would add a lot of code and allow edits we don't want. |
| `DateOnly` + `TimeOnly` instead of one `DateTime` | "Entries for a day" becomes simple `entry.Date == day`; the URL takes just a date. | With `DateTime` every comparison needs `.Date` and a time-zone decision. |
| `static class ActivityLogStore` + static constructor | One shared log; the static constructor adds sample entries once. | Same trade-off as 3.1's `ProductStore`. |
| `List<ActivityEntryModel>` returns (copies) | Copied inside the lock, then sorted/filtered. | Lazy `IEnumerable` would escape the lock (see 3.1). |
| `class CustomIsoDateFormatModelBinder : IModelBinder` | MVC asks for this interface; any class implementing it can bind a value. | — |
| `Task` return from `BindModelAsync` | The interface demands `Task`. We do no async work, so we return `Task.CompletedTask`. | — |
| `DateOnly? day` parameter | Missing or invalid → `null` → we fall back to today. | Non-nullable `DateOnly` would silently become `0001-01-01`. |
| `Task<IViewComponentResult> InvokeAsync` | The usual view-component shape; ready if the data ever comes from a database. | Data is in memory, so a plain `Invoke` would also work. `Task.FromResult` is used because nothing is awaited. |
| `ViewBag.Day`, `ViewBag.Error` | Two small extras next to the main `List<ActivityEntryModel>` model. | Same `ViewBag` trade-off as 3.1. |
