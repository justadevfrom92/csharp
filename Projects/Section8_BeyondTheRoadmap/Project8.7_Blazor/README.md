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

## Code snippets

**Interactive Server Blazor**, from `BlazorTodo/Program.cs` and `Components/App.razor`

```csharp
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

app.MapStaticAssets(); // also serves _framework/blazor.web.js
app.UseAntiforgery();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
```

```razor
<Routes @rendermode="InteractiveServer" />
```

**A page: route, form with validation, a loop, and `@code` state**, from `Components/Pages/TodoPage.razor`

```razor
@page "/todo"

<EditForm Model="newItem" OnValidSubmit="AddItem" FormName="add-todo">
    <DataAnnotationsValidator />
    <InputText @bind-Value="newItem.Title" placeholder="What needs doing?" />
    <button type="submit">Add</button>
    <ValidationMessage For="() => newItem.Title" />
</EditForm>

@foreach (TodoItemModel item in items)
{
    <TodoItemComponent @key="item.Id" Item="item" OnToggled="ToggleItem" OnRemoved="RemoveItem" />
}
<button disabled="@(CompletedCount == 0)" @onclick="ClearCompleted">Clear completed</button>

@code {
    private readonly List<TodoItemModel> items = new();
    private TodoItemModel newItem = new();

    private void AddItem()
    {
        items.Add(newItem);
        newItem = new TodoItemModel(); // empty the form
    }
}
```

**A reusable component: `[Parameter]` in, `EventCallback` out**, from `Components/TodoItemComponent.razor`

```razor
<li class="@(Item.IsDone ? "done" : "")">
    <input type="checkbox" checked="@Item.IsDone" @onchange="ToggleAsync" />
    <span>@Item.Title</span>
    <button class="remove" @onclick="RemoveAsync">Remove</button>
</li>

@code {
    [Parameter, EditorRequired]
    public TodoItemModel Item { get; set; } = null!;

    [Parameter]
    public EventCallback<TodoItemModel> OnToggled { get; set; }

    private Task ToggleAsync()
    {
        return OnToggled.InvokeAsync(Item);
    }
}
```

**Testing a component with bUnit**, from `BlazorTodo.Tests/TodoItemComponentTest.cs`

```csharp
public class TodoItemComponentTest : BunitContext
{
    [Fact]
    public void TodoItem_PositivePositive_RendersTitleAndRaisesCallbacks()
    {
        var item = new TodoItemModel { Title = "Buy milk" };
        TodoItemModel? toggled = null;

        IRenderedComponent<TodoItemComponent> component = Render<TodoItemComponent>(parameters => parameters
            .Add(child => child.Item, item)
            .Add(child => child.OnToggled, (TodoItemModel changed) => toggled = changed));
        component.Find("input[type=checkbox]").Change(true);

        Assert.Equal("Buy milk", component.Find("span").TextContent);
        Assert.Same(item, toggled);
    }
}
```

## Variations

Component variations in `BlazorTodo/Components/Variations/`, rendered by `BlazorTodo.Tests/VariationsTest.cs` (bUnit).

**Child tells parent "clicked"** (child clicked twice)

| Child parameter | Parent's counter | Parent's screen | The difference |
|---|---|---|---|
| `EventCallback OnClicked` (original style, `ChildEventCallbackComponent`) | 2 | **2** | Blazor re-renders the parent after the callback |
| `Action? OnClicked` (`ChildActionComponent`) | 2 | **0** | the method ran, but Blazor doesn't know the **parent** changed — its screen is stale |
| either, with no handler given | — | no crash | `EventCallback` is safe empty; `Action?` needs `?.Invoke` |

**Binding a text box** (typing "Ann")

| Binding | While typing | After leaving the box | The difference |
|---|---|---|---|
| `@bind="name"` (`BindOnChangeComponent`) | nothing — there is **no `oninput` handler at all** (bUnit: "only an onchange handler") | "Ann" | updates on the `change` event |
| `@bind="name" @bind:event="oninput"` (`BindOnInputComponent`) | "Ann" immediately | "Ann" | every keystroke re-renders: live search, character counters |
| `@bind="name" @bind:after="Saved"` (`BindAfterComponent`) | — | "Ann", then `Saved()` runs | run code after each change (2 changes = saved 2 times) without writing `value` + `@onchange` by hand |

**Render modes** (documented — the original uses Interactive Server):

| Mode | Where C# runs | The difference |
|---|---|---|
| Static server rendering (no `@rendermode`) | server, once | plain HTML; buttons don't run C# (forms still post) |
| `InteractiveServer` (original) | server, over a SignalR connection | fast start, needs a live connection; every click is a round trip |
| `InteractiveWebAssembly` | in the browser (.NET in WebAssembly) | works offline after download; bigger first load; no direct database access |
| `InteractiveAuto` | server first, then WebAssembly once downloaded | best first load + offline later; the component must work in both |
