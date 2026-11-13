# Cheat Sheet — Section 3: ASP.NET Core MVC

Projects: [3.1](../Section3_AspNetCoreMvc/Project3.1_ProductCatalog), [3.2](../Section3_AspNetCoreMvc/Project3.2_SidebarAndDateBinder), [3.3](../Section3_AspNetCoreMvc/Project3.3_FileUploadPortal).
See also [RAZOR_BASICS.md](../RAZOR_BASICS.md).

## `Program.cs` for MVC

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
var app = builder.Build();
app.UseStaticFiles();
app.UseRouting();
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
app.Run();
```

## A controller

```csharp
public class ProductsController : Controller
{
    private readonly ProductService service;

    public ProductsController(ProductService service) // dependency injection
    {
        this.service = service;
    }

    // GET /Products
    public IActionResult Index()
    {
        return View(service.GetAll());               // Views/Products/Index.cshtml
    }

    [HttpGet("/product/{id:int}")]                    // attribute route
    public IActionResult Details(int id)
    {
        ProductModel? product = service.Find(id);
        if (product == null)
        {
            return NotFound();
        }
        return View(product);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(ProductModel product)  // model binding fills it
    {
        if (!ModelState.IsValid)
        {
            return View(product);                     // show errors
        }
        service.Add(product);
        TempData["Message"] = "Saved.";               // survives one redirect
        return RedirectToAction(nameof(Index));       // Post/Redirect/Get
    }
}
```

## Action results

| Return | Gives |
|---|---|
| `View()` / `View(model)` / `View("Name", model)` | an HTML page |
| `RedirectToAction(nameof(Index))` | 302 to another action |
| `Redirect(url)` / `LocalRedirect(url)` | 302 to a URL (local only for the second) |
| `NotFound()`, `BadRequest()`, `Forbid()`, `Unauthorized()` | 404, 400, 403, 401 |
| `Ok(data)`, `Json(data)` | JSON |
| `Content("text", "text/plain")` | plain text |
| `File(bytes, "application/pdf", "report.pdf")` | a download |
| `PartialView("_Row", model)` | a piece of HTML |

## Where parameters come from

```csharp
public IActionResult Search(
    [FromQuery] string term,          // ?term=...
    [FromRoute] int id,               // /search/5
    [FromForm] string note,           // posted form field
    [FromHeader(Name = "X-Id")] string clientId,
    [FromBody] SearchRequest body)    // JSON body (APIs)
```

Without attributes, MVC looks in route, query and form by **name**.

## Validation attributes

```csharp
public class ProductModel
{
    [Required(ErrorMessage = "Name is required.")]
    [StringLength(50, MinimumLength = 2)]
    public string Name { get; set; } = "";

    [Range(0.01, 100000)]
    public decimal Price { get; set; }

    [EmailAddress]
    public string? Email { get; set; }

    [Url]
    public string? Website { get; set; }

    [RegularExpression("^[A-Z]{2}-[0-9]{2}$")]
    public string Code { get; set; } = "";

    [Display(Name = "Date of birth")]
    public DateOnly BirthDate { get; set; }
}

ModelState.AddModelError(nameof(ProductModel.Name), "That name is taken."); // your own rule
```

## A custom validation attribute

```csharp
public class NotInFutureAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is DateOnly date && date > DateOnly.FromDateTime(DateTime.Today))
        {
            return new ValidationResult("Date can't be in the future.");
        }
        return ValidationResult.Success;
    }
}
```

## Razor view basics

```cshtml
@model List<ProductModel>
@{
    ViewData["Title"] = "Products";
}
<h1>@ViewData["Title"]</h1>

@if (Model.Count == 0)
{
    <p>No products.</p>
}
@foreach (ProductModel product in Model)
{
    <p>@product.Name - @product.Price.ToString("0.00")</p>
}
<p>@(Model.Count * 2)</p>
@* a Razor comment, not sent to the browser *@
```

## Tag helpers

```cshtml
<a asp-controller="Products" asp-action="Details" asp-route-id="5">View</a>

<form asp-action="Create" method="post">
    <div asp-validation-summary="ModelOnly"></div>
    <label asp-for="Name"></label>
    <input asp-for="Name" />
    <span asp-validation-for="Name"></span>
    <select asp-for="Category" asp-items="@(new SelectList(ViewBag.Categories))"></select>
    <button type="submit">Save</button>
</form>

<form asp-action="Upload" method="post" enctype="multipart/form-data">
    <input asp-for="File" type="file" />
</form>

<partial name="_ProductRow" model="product" />
<vc:recent-activity-sidebar count="5" />
<link rel="stylesheet" href="~/css/site.css" />
```

## Layout, partials, view components

```cshtml
@* Views/Shared/_Layout.cshtml *@
<html>
<body>
    @if (TempData["Message"] is string message)
    {
        <div class="alert">@message</div>
    }
    @RenderBody()
    @await RenderSectionAsync("Scripts", required: false)
</body>
</html>
```

```csharp
public class CartSummaryViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(int maxItems)
    {
        return View(LoadItems(maxItems)); // Views/Shared/Components/CartSummary/Default.cshtml
    }
}
```

## File upload

```csharp
public async Task<IActionResult> Upload(IFormFile file)
{
    string safeName = Guid.NewGuid().ToString("N") + Path.GetExtension(file.FileName).ToLowerInvariant();
    await using FileStream stream = System.IO.File.Create(Path.Combine(uploadFolder, safeName));
    await file.CopyToAsync(stream);
    return RedirectToAction(nameof(Index));
}
```
