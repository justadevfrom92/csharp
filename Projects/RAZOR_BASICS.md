# Razor Basics — how `.cshtml` files work

A `.cshtml` file is an **HTML page with C# mixed in**. The format is called **Razor**.
It runs **on the server**: the C# parts are executed, and the browser only receives the
finished, plain HTML. (Open any page and use "View page source": no `@`, no `asp-`, no C#.)

Examples below are from Project 3.3 (upload portal) and 3.1 (product catalog).

---

## 1. How a controller and a view connect

```
Browser asks for /Upload/Avatar
        │
        ▼
UploadController.Avatar()                         Controllers/UploadController.cs
    return View(new AvatarUploadViewModel());          ← creates the data and picks the view
        │
        ▼
Views/Upload/Avatar.cshtml                        ← found by NAME: Views/{Controller}/{Action}.cshtml
    @model AvatarUploadViewModel                       ← "I receive an AvatarUploadViewModel"
    ... HTML using Model ...
        │
        ▼
Plain HTML sent to the browser
```

- `return View();` → renders `Views/<ControllerName>/<ActionName>.cshtml`.
- `return View(something);` → same file, and `something` becomes the view's **`Model`**.
- `return View("Index", something);` → a view with a different name.
- If no file is found in `Views/Upload/`, MVC also looks in `Views/Shared/`.

---

## 2. `@model` and `Model`

```cshtml
@model AvatarUploadViewModel
```

The first line of a view says **what type of object** the controller passes in.
- In the view, that object is called **`Model`** (capital M): `Model.Avatar`, `Model.Count`, ...
- The compiler checks it: `Model.Avtar` (typo) is a build error.
- `asp-for="Avatar"` also uses it: it means "the `Avatar` property of the model".

| View | `@model` | Controller passes |
|---|---|---|
| `Upload/Index.cshtml` | `List<string>` | `return View(savedFileNames);` |
| `Products/Create.cshtml` (3.1) | `ProductFormViewModel` | `return View(form);` |
| `Upload/Avatar.cshtml` | `AvatarUploadViewModel` | `return View(new AvatarUploadViewModel());` |
| `Products/Index.cshtml` (3.1) | `List<Product>` | `return View(ProductStore.GetAll());` |

Where are those classes? A `...ViewModel` is always in the project's `Models/` folder, **one class
per file, file named after the class**: `AvatarUploadViewModel` → `Models/AvatarUploadViewModel.cs`.
Entities like `Product` are in `Data/Entities/`. Full layout: [FOLDER_STRUCTURE.md](FOLDER_STRUCTURE.md).

---

## 3. The other direction: form → object (model binding)

```csharp
[HttpPost]
public async Task<IActionResult> Avatar(AvatarUploadViewModel form)
```

Nobody writes `new AvatarUploadViewModel()` for the POST. **MVC creates it**:
1. The browser posts the form fields: a file named `Avatar` (the name comes from `asp-for="Avatar"`).
2. MVC sees the parameter type `AvatarUploadViewModel`, creates one, and copies every posted field into
   the property with the **same name**.
3. It runs the validation attributes (`[Required]`, `[MaximumFileSizeValidation(1)]`, ...) and puts the
   result in `ModelState`. `ModelState.IsValid` is `false` if any failed.

This is called **model binding**. The names in the HTML and the property names in the class must
match — which is exactly what `asp-for` guarantees.

---

## 4. The `@` symbol: switching from HTML to C#

| You write | What it does |
|---|---|
| `@fileName` | Prints a C# value into the HTML. **HTML-encoded** automatically: `<script>` shows as text. |
| `@product.Price.ToString("0.00")` | Prints the result of a longer expression. |
| `@(Model.Description ?? "No description.")` | `@( ... )` for expressions with spaces or operators. |
| `@if (Model.Count == 0) { <p>No files yet.</p> }` | Normal C# `if`. The HTML inside is only written when true. |
| `@foreach (string fileName in Model) { <li>@fileName</li> }` | Normal C# loop. The HTML inside is written once per item. |
| `@{ ViewData["Title"] = "Products"; }` | A block of plain C# statements (no output). |
| `@* comment *@` | Razor comment. **Not** sent to the browser (unlike `<!-- -->`). Can't be nested. |
| `@Html.Raw(text)` | Prints **without** encoding. Never use it on user input (XSS — see 7.2). |

Inside `{ }` of an `@if`/`@foreach` you go straight back to writing HTML; Razor works out
where the C# ends and the HTML begins.

---

## 5. `ViewBag`, `ViewData`, `TempData`

Besides the model, MVC has three loose "bags" for passing values to a view:

| | Example | Lives for | Type-checked? |
|---|---|---|---|
| `Model` | `@model CartPageViewModel` | this request | **Yes** |
| `ViewBag.X` | `ViewBag.Categories = ...` | this request | **No** — it's `dynamic`, typos only fail when the page runs |
| `ViewData["X"]` | `ViewData["Title"] = "Products"` | this request | No (same storage as `ViewBag`, string keys) |
| `TempData["X"]` | message after a save | **until it's read, even across a redirect** | No |

Rules used in these projects:
- **Everything a page shows goes in its view model** (`Models/...ViewModel.cs`) — including small extras
  like dropdown choices or an error message. `ViewBag` is **not used anywhere**, because the compiler
  can't check it.
- **`ViewData["Title"]`** is used only for the `<title>` in 3.1's layout (the one standard use).
- **`TempData`** only for a message that must survive a redirect (read in `_Layout.cshtml`, see 3.1).

---

## 6. The special files

| File | What it does |
|---|---|
| `Views/_ViewImports.cshtml` | Lines added to **every** view: `@using` (namespaces), `@addTagHelper` (turns on `asp-*`), `@inject` (services, see 7.5). |
| `Views/_ViewStart.cshtml` | Runs before every view. Ours just sets `Layout = "_Layout";`. |
| `Views/Shared/_Layout.cshtml` | The page frame: `<html>`, `<head>`, menu, footer. **`@RenderBody()`** is where each view's own HTML is inserted. |
| `Views/Shared/_Something.cshtml` | A **partial**: a reusable piece of HTML, inserted with `<partial name="_Something" />`. The `_` is just a naming habit for "not a full page". |
| `Views/Shared/Components/<Name>/Default.cshtml` | The HTML for a **view component** (3.2 sidebar). |

So a finished page is: `_Layout.cshtml` (frame) + the action's view (inserted at `@RenderBody()`)
+ any partials and view components inside them.

---

## 7. `asp-*` attributes

`asp-for`, `asp-action`, `asp-route-id`, ... are **Tag Helpers**: server-side code that turns those
tags into normal HTML (building URLs, `name=` attributes, anti-forgery tokens, error messages).
Full table: [WHY_THESE_TYPES.md, section 8](WHY_THESE_TYPES.md#8-asp--attributes-tag-helpers-in-the-cshtml-files).
Every view that uses them also has a comment explaining them.

---

## 8. `@inject` (Project 7.5)

```cshtml
@inject FeatureFlagService FeatureFlags
```

Gets a service from dependency injection straight into the view, the same way a controller
gets it through its constructor. Used for things every page needs (feature flags). Avoid it for
page data — that should come through the model.
