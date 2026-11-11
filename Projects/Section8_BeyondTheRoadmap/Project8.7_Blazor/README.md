# Project 8.7 — Blazor (Interactive Server)

| Folder | What |
|---|---|
| `BlazorTodo/` | A Blazor Web App: a to-do list written entirely in C# + HTML components |
| `BlazorTodo.Tests/` | bUnit tests: components rendered in memory, clicked and checked — no browser |

```bash
dotnet run --project BlazorTodo
```
Open `http://localhost:5087/todo`.

```bash
dotnet test BlazorTodo.Tests
```

## What Blazor is

A web UI written in **C# instead of JavaScript**. A page is a **component** (`.razor` file): HTML plus a
`@code { }` block. Clicking a button calls a C# method; changing a field and returning makes Blazor
**re-render** and update only the parts of the page that changed.

**Render modes** (`@rendermode` in `Components/App.razor`):

| Mode | Where the C# runs | Used here? |
|---|---|---|
| Static server (default) | on the server, once — plain HTML, no interactivity | — |
| **Interactive Server** | on the server; each click travels over a live **SignalR** connection | **yes** |
| Interactive WebAssembly | **in the browser** (.NET downloaded as WebAssembly) | — |
| Interactive Auto | Server first, WebAssembly once it's downloaded | — |

## The pieces

| File | What it shows |
|---|---|
| `Components/App.razor` | The HTML page shell; `<Routes @rendermode="InteractiveServer" />` turns on interactivity |
| `Components/Routes.razor` | Picks the component whose `@page` matches the URL |
| `Components/Layouts/MainLayout.razor` | Shared layout; `@Body` is where the page goes |
| `Components/Pages/TodoPage.razor` | `@page "/todo"`, **state** (`items` list), `EditForm` + `DataAnnotationsValidator`, `@foreach` with `@key` |
| `Components/TodoItemComponent.razor` | A **reusable child**: `[Parameter] Item` in, `EventCallback<TodoItemModel>` out |
| `Models/TodoItemModel.cs` | `[Required]`, `[StringLength(60)]` — checked by the form |

**Parent/child rule used here:** the child (`TodoItemComponent`) never changes the item itself. It only *reports*
"toggle me" / "remove me" through an `EventCallback`, and the parent (`TodoPage`), which owns the list, decides.
One owner per piece of state keeps the UI predictable.

**`@key`** tells Blazor which `<li>` belongs to which item, so removing item 1 doesn't make item 2's checkbox
"move up" and look wrong.

## Problems found while building this

1. **`blazor.web.js` → 404** with `UseStaticFiles()`. Since .NET 9, framework files like `_framework/blazor.web.js`
   are served by **`app.MapStaticAssets()`**. Fixed.
2. **Then a 500 when run with `dotnet run --no-build` in Production.** Outside `publish`, files from packages are
   only mapped in the **Development** environment. Added `Properties/launchSettings.json` (Development). The
   **published** app works in Production (checked: `dotnet publish`, then run with `ASPNETCORE_ENVIRONMENT=Production`).

Both were caught by driving the app with a **real browser** (headless Chromium over the live connection): add two
items → "2 of 2 left", tick one → "1 of 2 left" with the `done` class, empty submit → "Write what needs doing.",
clear completed → "1 of 1 left".

## Tests (every behaviour has all three kinds)

| Behaviour | PositivePositive | PositiveNegative | NegativeNegative |
|---|---|---|---|
| Add item | "  Buy milk  " → listed (trimmed), form cleared | 61 characters → "Keep it under 60 characters.", nothing added | empty or only spaces → "Write what needs doing.", nothing added |
| Complete item (child → parent callback) | tick → "1 of 2 left", `done` class | tick twice → back to not done | nothing completed → "Clear completed" is **disabled** |
| Remove / clear | removes only that item | removing the last item → "Nothing to do." | clear completed keeps the not-done items |
| `TodoItemComponent` alone | shows the title, raises both callbacks with the right item | a done item renders checked with `done` | no callbacks given → clicking doesn't crash and doesn't change the item |
