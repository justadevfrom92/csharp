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
