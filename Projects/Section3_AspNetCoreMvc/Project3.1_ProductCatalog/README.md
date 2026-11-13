# Project 3.1 — MVC Product Catalog

> New to `.cshtml` files (`@model`, `@foreach`, `ViewBag`, layouts)? Read [RAZOR_BASICS.md](../../RAZOR_BASICS.md) first.

A small ASP.NET Core MVC site to list, add, view and edit products.

```bash
dotnet run      # then open the URL it prints
```

## What it covers

- **Conventional routing** (`{controller=Products}/{action=Index}/{id?}`) and one
  **attribute route** (`/product/{id}` on `Details`).
- **GET and POST actions**: `Create` and `Edit` each have a GET (show form) and a POST (save).
- **Validation**: `[Required]`, `[Range]`, `[StringLength]` on `ProductModel`. If `ModelState` is not
  valid, the same view is returned and the tag helpers show the messages.
- **Tag Helpers**: `<form asp-action>`, `<input asp-for>`, `<select asp-items>`, `asp-validation-for`.
- **Partial views**: `_Header`, `_Navigation`, `_Footer` in the layout, and `_ProductFormFields`
  shared by Create and Edit.
- **Post/Redirect/Get**: after a good POST we redirect and show a message with `TempData`.

## Why these choices

**Why partial views and not view components for the header/footer/nav.**
A partial view is just a piece of HTML pasted into the page. The header, footer and menu are
plain HTML with no data to load, so a partial is all they need. A view component is a
partial *plus a C# class* that can run code and load data — that's useful for something like
"recent activity" (see 3.2), but for static HTML it would be an extra class doing nothing.

**Why `_ProductFormFields` is a partial.** The Create and Edit forms have the same fields.
Writing them once means a new field only needs to be added in one place.

**Why redirect after POST.** If we returned the page directly after saving, pressing F5 would
ask the browser to send the form again and add the product twice. Redirecting avoids that.

**Why a static list instead of a database.** This project is about MVC, not storage.
The list has a `lock` because web requests run at the same time on different threads.
Databases come in Section 6.

## Code snippets

**Conventional routing**, from `Program.cs`

```csharp
builder.Services.AddControllersWithViews();
var app = builder.Build();
app.UseStaticFiles();
app.UseRouting();
app.MapControllerRoute("default", "{controller=Products}/{action=Index}/{id?}"); // "/" -> Products/Index
app.Run();
```

**Attribute routing + `NotFound()`**, from `Controllers/ProductsController.cs`

```csharp
[HttpGet("/product/{id:int}")] // GET /product/5
public IActionResult Details(int id)
{
    ProductModel? product = ProductStore.FindById(id);
    if (product == null)
    {
        return NotFound();
    }
    return View(product);
}
```

**POST: validate, then redirect with a `TempData` message (Post/Redirect/Get)**

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public IActionResult Create(ProductModel product)
{
    if (!ModelState.IsValid)
    {
        ViewBag.Categories = ProductStore.Categories;
        return View(product); // show the form again with the error messages
    }
    ProductStore.Add(product);
    TempData["SuccessMessage"] = $"Product '{product.Name}' was added.";
    return RedirectToAction(nameof(Index));
}
```

**Validation attributes on the model**, from `Models/ProductModel.cs`

```csharp
[Required(ErrorMessage = "Name is required.")]
[StringLength(50, MinimumLength = 2, ErrorMessage = "Name must be 2 to 50 characters.")]
public string Name { get; set; } = "";

[Range(0.01, 100000, ErrorMessage = "Price must be between 0.01 and 100,000.")]
public decimal Price { get; set; }
```

**A form with tag helpers and a partial**, from `Views/Products/Create.cshtml` and `_ProductFormFields.cshtml`

```cshtml
<form asp-action="Create" method="post">
    <partial name="_ProductFormFields" model="Model" />
    <button type="submit">Save</button>
</form>

<div asp-validation-summary="ModelOnly"></div>
<label asp-for="Name"></label>
<input asp-for="Name" />
<span asp-validation-for="Name"></span>
```

**Reading `TempData` once, in the layout**, from `Views/Shared/_Layout.cshtml`

```cshtml
@if (TempData["SuccessMessage"] is string successMessage)
{
    <div class="success-alert">@successMessage</div>
}
```

**Thread-safe in-memory store: copy inside the `lock`**, from `Stores/ProductStore.cs`

```csharp
private static readonly object listLock = new();

public static List<ProductModel> GetAll()
{
    lock (listLock)
    {
        return products.ToList();
    }
}
```

## Why these types

General rules: [WHY_THESE_TYPES.md](../../WHY_THESE_TYPES.md). Choices made in this project:

| Choice | Why | Why not the alternative |
|---|---|---|
| `class ProductModel` with `{ get; set; }` | MVC model binding creates an empty object and sets each form field. Needs a mutable class. | A `record` with a constructor works for binding but fights with partial edits and validation attributes. |
| `static class ProductStore` | One shared list for the whole app, nothing to configure. Simplest thing that works. | A DI singleton (shown in 6.3) is easier to swap in tests. Static is fine for a demo without a database. |
| `private static readonly List<ProductModel>` | Must grow (`Add`). `readonly` stops anyone replacing the whole list by mistake. | Array can't grow. |
| `static readonly string[] Categories` | Fixed set of 3 categories. | `List` suggests categories can be added at runtime. |
| `object listLock` | Requests run on many threads; `List<T>` isn't thread-safe. A private object only used for locking. | `lock (products)` works but anyone holding the list could lock it too. |
| `GetAll()` returns `List<ProductModel>` (a copy) | The copy is made **inside the lock**. The caller gets its own list. | Returning `IEnumerable` would run *after* the lock is released while another request adds → "Collection was modified" crash. |
| `ProductModel? FindById` | "Not found" is normal (bad id in URL) → `null`, and the controller returns 404. | Throwing an exception for a normal case is slow and noisy. |
| `bool Update` | Same idea: `false` = not found. | — |
| `IActionResult` return type | Actions return `View`, `NotFound` or `Redirect` — different classes. | A specific type like `ViewResult` couldn't return `NotFound()`. |
| `@model ProductModel` / `@model List<ProductModel>` | Main page data, checked by the compiler. | — |
| `ViewBag.Categories` | A small side list for the dropdown. | **Trade-off:** `ViewBag` is `dynamic`, so a typo is a runtime error. A `ProductFormViewModel` class with a `Categories` property would be safer; kept as `ViewBag` to keep the demo short. |
| `TempData["SuccessMessage"]` | The message has to survive the redirect after a save. | `ViewBag` is gone after a redirect. |
| `public partial class Program` | Lets the Playwright tests (4.3) reference the app's entry point. | Without it, top-level `Program` is not visible to other projects. |
