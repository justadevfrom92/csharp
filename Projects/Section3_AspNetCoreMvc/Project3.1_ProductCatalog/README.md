# Project 3.1 — MVC Product Catalog

A small ASP.NET Core MVC site to list, add, view and edit products.

```bash
dotnet run      # then open the URL it prints
```

## What it covers

- **Conventional routing** (`{controller=Products}/{action=Index}/{id?}`) and one
  **attribute route** (`/product/{id}` on `Details`).
- **GET and POST actions**: `Create` and `Edit` each have a GET (show form) and a POST (save).
- **Validation**: `[Required]`, `[Range]`, `[StringLength]` on `Product`. If `ModelState` is not
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
