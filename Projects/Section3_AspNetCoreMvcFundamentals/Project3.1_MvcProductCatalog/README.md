# Mini-Project 3.1 — MVC Product Catalog & Form Submission App

A multi-page ASP.NET Core MVC app for listing, filtering, creating, editing and deleting
products. It is also the system-under-test for the Playwright suite in **Project 4.3**.

## Run

```bash
dotnet run            # http://localhost:5031
```

| URL | Routing style | Action |
|---|---|---|
| `/`, `/Home/About` | Conventional (`{controller=Home}/{action=Index}/{id?}`) | `HomeController` |
| `GET /products?searchText=&category=` | Attribute | List + filter (GET form) |
| `GET/POST /products/create` | Attribute | Create form |
| `GET /products/{productId:int}` | Attribute | Details |
| `GET/POST /products/{productId:int}/edit` | Attribute | Edit form |
| `POST /products/{productId:int}/delete` | Attribute | Delete |
| `/status/{code}` | Attribute (all verbs) | Friendly 400/404/500 page via `UseStatusCodePagesWithReExecute` |

## Request flow

`Kestrel → StaticFiles → Routing → Antiforgery → Endpoint (Controller action) → IActionResult
(ViewResult → Razor view + layout + partials | RedirectToActionResult | NotFoundResult)`.

## Design decisions

* **View model vs entity.** Forms bind to `ProductEditorFormViewModel`, never to `CatalogProduct`.
  This blocks over-posting: a malicious form cannot set `ProductId` or `CreatedAt`. It also keeps
  *input* validation rules separate from the storage shape.
* **Validation in two layers.** DataAnnotations (`[Required]`, `[StringLength]`, `[Range]`) cover
  stateless rules. Rules that need data, such as "name must be unique" or "available products need stock",
  cannot be attributes, so the controller adds them to `ModelState`. Either way the view re-renders
  with `asp-validation-for` messages and the user's posted values preserved.
* **`decimal?` and `ProductCategory?` on the view model.** Nullable types let `[Required]`
  distinguish "not supplied" from `0`/the first enum member. With a non-nullable `decimal`, an
  empty price box would silently bind as `0`.
* **Post/Redirect/Get + `TempData`.** Successful POSTs redirect, so refreshing the page never
  re-submits. The success message travels in `TempData`, which lives for exactly one subsequent request.
* **Anti-forgery.** The `<form>` tag helper emits the token automatically, and every POST action has
  `[ValidateAntiForgeryToken]`. A POST without a token returns **400** (verified in the smoke test).
* **Attribute routing for the resource controller.** Explicit URLs such as `/products/7/edit` are
  stable and readable. The `{productId:int}` constraint rejects `/products/abc` with 404 before model binding.
* **Singleton in-memory repository behind `IProductCatalogRepository`.** The interface lets a
  real database (Section 6) replace it without touching the controller. One `Lock` guards all
  access because concurrent requests share the singleton.
* **Status-code pages accept every verb.** A re-executed request keeps its original method. If
  the status page were GET-only, a failed POST would turn a 400 into a confusing 405. This bug was found
  during smoke testing and fixed by giving the action no `[HttpGet]`.

## Why partial views (not view components) for the header, navigation and footer (the required write-up)

| Concern | Partial view | View component |
|---|---|---|
| Needs its own data access / services? | No: it renders from the parent's `ViewContext` | Yes: `InvokeAsync` can inject and query services |
| Extra class + DI resolution per render | None | One class instantiated per invocation |
| Testable logic inside | None needed | Yes, it is a class |
| Typical use | Static or near-static markup shared across views | Self-contained widget with its own data (see Project 3.2) |

The header, footer and navigation are **pure presentation**. They show fixed markup, and the nav
only reads route values that are already on the request to highlight the active link. Nothing
needs to be fetched, so a view component would add a class, DI resolution and an
`InvokeAsync` call per render for no benefit. Partials cost nothing extra, are edited as plain
Razor, and keep the layout readable (`<partial name="_SiteHeaderPartial" />`).

The same reasoning applies to `_ProductEditorFormFieldsPartial`. The Create and Edit forms must have
identical fields and validation output. Sharing one partial guarantees that, while each outer
view keeps its own `asp-action` and button text. If any of these blocks later needs its *own*
data, such as a cart count in the header, that block, and only that block, should become a view component.
