# Project 8.6 — Razor Pages

> New to `.cshtml` files? Read [RAZOR_BASICS.md](../../RAZOR_BASICS.md) first.

| Folder | What |
|---|---|
| `RazorPagesContacts/` | A contacts list with add, edit and delete — written with Razor Pages |
| `RazorPagesContacts.Tests/` | Tests that post the real forms (with anti-forgery tokens) |

```bash
dotnet run --project RazorPagesContacts
```

```bash
dotnet test RazorPagesContacts.Tests
```

## Razor Pages vs MVC (Project 3.1)

| | MVC | Razor Pages |
|---|---|---|
| Where the code goes | a **controller** with many actions, views in `Views/<Controller>/` | each **page** has its own `.cshtml` + `.cshtml.cs` next to each other in `Pages/` |
| URL comes from | routing rules (`{controller}/{action}/{id?}`) | the **file path**: `Pages/Contacts/Edit.cshtml` → `/Contacts/Edit` |
| Handling a request | action methods (`Create()`, `[HttpPost] Create(...)`) | `OnGet()`, `OnPost()`, named handlers like `OnPostDelete()` |
| Form data | action parameters | `[BindProperty]` properties on the page class |
| Anti-forgery | `[ValidateAntiForgeryToken]` (or a global filter) | **on automatically** for every POST |
| Good for | apps with many shared actions, APIs + views | page-focused sites: forms, admin screens, CRUD |

## What's in it

| Piece | Where |
|---|---|
| `@page` — makes a `.cshtml` a page with its own URL | every page in `Pages/Contacts/` |
| `@page "{id:int}"` — a route value in the URL (`/Contacts/Edit/1`) | `Edit.cshtml` |
| `PageModel` class with `OnGet` / `OnPost` | `*.cshtml.cs` |
| `[BindProperty] public ContactModel Contact` — form fields bound into it on POST | `Create`, `Edit` |
| **Named handler**: `asp-page-handler="Delete"` → `OnPostDelete()` | `Edit` (a second form on the same page) |
| `Page()` (show again with errors), `RedirectToPage("./Index")` (Post/Redirect/Get), `NotFound()` | `Create`, `Edit` |
| `asp-page` links instead of `asp-controller`/`asp-action` | `_Layout`, `Index` |

**A safety detail:** on Edit, the id is taken from the **URL**, not from the hidden form field. Someone changing the
hidden `Contact.Id` can't edit a different contact (there's a test for this).

**Folder note:** Razor Pages *requires* each page's C# class to sit next to its `.cshtml` in `Pages/`, so — like
`Views/` — this folder is a framework-mandated exception to the "class name ends in its folder name" rule. The
classes are named `Contact...PageModel`.

## Tests (every behaviour has all three kinds)

| Behaviour | PositivePositive | PositiveNegative | NegativeNegative |
|---|---|---|---|
| Create | valid → 302 to `/Contacts`, listed | 1-letter name, bad email, bad phone → same page with all 3 errors, nothing added | no anti-forgery token → 400, nothing added |
| Edit | valid change → saved | `/Contacts/Edit/9999` → 404; hidden id changed to 1 → contact 1 untouched | `/Contacts/Edit/abc` → 404 |
| Delete (named handler) | POST `?handler=Delete` → removed | already deleted → 404 | a GET to the delete handler → nothing deleted |

## Code snippets

**Razor Pages instead of controllers**, from `RazorPagesContacts/Program.cs`

```csharp
builder.Services.AddRazorPages();
app.MapRazorPages(); // every .cshtml under Pages/ with "@page" becomes a URL
```

**The page class: `[BindProperty]`, `OnGet`, `OnPost`**, from `Pages/Contacts/Create.cshtml.cs`

```csharp
public class ContactCreatePageModel : PageModel
{
    [BindProperty]
    public ContactModel Contact { get; set; } = new();

    public void OnGet()
    {
    }

    public IActionResult OnPost()
    {
        if (!ModelState.IsValid)
        {
            return Page(); // same page again, with the error messages
        }
        store.Add(Contact);
        TempData["Message"] = $"Added {Contact.Name}.";
        return RedirectToPage("./Index");
    }
}
```

**The page markup**, from `Pages/Contacts/Create.cshtml`

```cshtml
@page
@model ContactCreatePageModel

<form method="post">
    <label asp-for="Contact.Name"></label>
    <input asp-for="Contact.Name" />
    <span asp-validation-for="Contact.Name" class="error"></span>
    <button type="submit">Save</button>
</form>
```

**A route value in the page, and a named handler for a second form**, from `Pages/Contacts/Edit.cshtml(.cs)`

```cshtml
@page "{id:int}"

<form method="post" asp-page-handler="Delete" asp-route-id="@Model.Contact.Id">
```

```csharp
public IActionResult OnGet(int id)          // GET  /Contacts/Edit/1
public IActionResult OnPost(int id)         // POST /Contacts/Edit/1
public IActionResult OnPostDelete(int id)   // POST /Contacts/Edit/1?handler=Delete
```

**Link to a page**, from `Pages/Contacts/Index.cshtml`

```cshtml
<a asp-page="/Contacts/Edit" asp-route-id="@contact.Id">Edit</a>
```

## Variations

Four ways for a page to receive a search term, in `RazorPagesContacts/Pages/Variations/`.
`RazorPagesContacts.Tests/VariationsTest.cs` searches for "ada" (1 of the 2 seeded contacts) with each.

| Page | URL | Filtered? | Page can show the term? | The difference |
|---|---|---|---|---|
| `[BindProperty(SupportsGet = true)] string? Term` (`SearchWithSupportsGet`) | `?term=ada` | ✅ 1 result | ✅ | the usual way for search/filter pages |
| `@page "{term?}"` + `SupportsGet` (`SearchWithRouteValue`) | `/ada` | ✅ 1 result | ✅ | the value in the **path** (like the original Edit page's `{id:int}`) |
| `[BindProperty]` **without** `SupportsGet` (`SearchWithoutSupportsGet`) | `?term=ada` | ❌ **2 results** | ❌ | only binds on **POST** (right for the original Create/Edit forms) — on GET the term is silently ignored |
| handler parameter `OnGet(string? term)` (`SearchWithHandlerParameter`) | `?term=ada` | ✅ 1 result | ❌ | the value is a local variable; copy it to a property to show it |

**Other Razor Pages choices** (documented):

| Original | Alternative | The difference |
|---|---|---|
| `[BindProperty] public ContactModel Contact` | `OnPost(ContactModel contact)` parameter | same binding; the parameter isn't on the page for re-display after an error unless you copy it |
| `[BindProperty]` per property | `[BindProperties]` on the class | binds **every** public property — easy over-posting |
| `return RedirectToPage("./Index")` after a save | `return Page()` | Page() re-renders; refreshing the browser **posts the form again** |
| named handler `OnPostDelete` + `asp-page-handler="Delete"` | a separate Delete page | one page for edit + delete vs one page per action |
