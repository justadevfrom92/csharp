using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MvcProductCatalog.Models;
using MvcProductCatalog.Services;

namespace MvcProductCatalog.Controllers;

/// <summary>
/// Reached through ATTRIBUTE routing. Explicit templates give stable, readable URLs
/// (/products/7/edit) and route constraints ({productId:int}) reject bad URLs with 404
/// before the action runs.
/// </summary>
[Route("products")]
public sealed class ProductsController(IProductCatalogRepository productCatalogRepository) : Controller
{
    private const string StatusAlertTempDataKey = "StatusAlert";

    [HttpGet("")]
    public IActionResult Index([FromQuery] string? searchText, [FromQuery] ProductCategory? category)
    {
        IReadOnlyList<CatalogProduct> matchingProducts = productCatalogRepository.SearchProducts(searchText, category);
        ViewData["CategoryOptions"] = BuildCategorySelectList(category);
        return View(new ProductListPageViewModel(
            matchingProducts,
            searchText,
            category,
            matchingProducts.Sum(product => product.UnitPrice * product.StockQuantity)));
    }

    [HttpGet("{productId:int}")]
    public IActionResult Details(int productId)
    {
        CatalogProduct? product = productCatalogRepository.FindProductById(productId);
        return product is null ? NotFound() : View(product);
    }

    [HttpGet("create")]
    public IActionResult Create()
    {
        ViewData["CategoryOptions"] = BuildCategorySelectList(null);
        return View(new ProductEditorFormViewModel());
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public IActionResult Create(ProductEditorFormViewModel productForm)
    {
        AddBusinessRuleValidationErrors(productForm, excludedProductId: null);
        if (!ModelState.IsValid)
        {
            // Re-render the same view with the posted values and error messages (no redirect).
            ViewData["CategoryOptions"] = BuildCategorySelectList(productForm.Category);
            return View(productForm);
        }

        CatalogProduct createdProduct = productCatalogRepository.CreateProduct(productForm);
        SetStatusAlert("success", $"Product '{createdProduct.ProductName}' was created.");
        // Post/Redirect/Get: a browser refresh after this redirect cannot re-submit the form.
        return RedirectToAction(nameof(Details), new { productId = createdProduct.ProductId });
    }

    [HttpGet("{productId:int}/edit")]
    public IActionResult Edit(int productId)
    {
        CatalogProduct? product = productCatalogRepository.FindProductById(productId);
        if (product is null)
        {
            return NotFound();
        }
        ViewData["ProductId"] = productId;
        ViewData["CategoryOptions"] = BuildCategorySelectList(product.Category);
        return View(ProductEditorFormViewModel.FromProduct(product));
    }

    [HttpPost("{productId:int}/edit")]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(int productId, ProductEditorFormViewModel productForm)
    {
        if (productCatalogRepository.FindProductById(productId) is null)
        {
            return NotFound();
        }

        AddBusinessRuleValidationErrors(productForm, excludedProductId: productId);
        if (!ModelState.IsValid)
        {
            ViewData["ProductId"] = productId;
            ViewData["CategoryOptions"] = BuildCategorySelectList(productForm.Category);
            return View(productForm);
        }

        productCatalogRepository.TryUpdateProduct(productId, productForm);
        SetStatusAlert("success", $"Product '{productForm.ProductName}' was updated.");
        return RedirectToAction(nameof(Details), new { productId });
    }

    [HttpPost("{productId:int}/delete")]
    [ValidateAntiForgeryToken]
    public IActionResult Delete(int productId)
    {
        CatalogProduct? product = productCatalogRepository.FindProductById(productId);
        if (product is null || !productCatalogRepository.TryDeleteProduct(productId))
        {
            return NotFound();
        }
        SetStatusAlert("info", $"Product '{product.ProductName}' was deleted.");
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Rules that need data access cannot be attributes on the view model; they are added to ModelState here.</summary>
    private void AddBusinessRuleValidationErrors(ProductEditorFormViewModel productForm, int? excludedProductId)
    {
        if (!string.IsNullOrWhiteSpace(productForm.ProductName)
            && productCatalogRepository.IsProductNameTaken(productForm.ProductName, excludedProductId))
        {
            ModelState.AddModelError(nameof(ProductEditorFormViewModel.ProductName), "A product with this name already exists.");
        }
        if (productForm.IsAvailableForSale && productForm.StockQuantity == 0)
        {
            ModelState.AddModelError(nameof(ProductEditorFormViewModel.StockQuantity), "A product available for sale must have stock.");
        }
    }

    private void SetStatusAlert(string alertKind, string messageText)
    {
        // TempData survives exactly one redirect, which is what a post-redirect confirmation needs.
        TempData[$"{StatusAlertTempDataKey}Kind"] = alertKind;
        TempData[$"{StatusAlertTempDataKey}Text"] = messageText;
    }

    private static SelectList BuildCategorySelectList(ProductCategory? selectedCategory) =>
        new(Enum.GetValues<ProductCategory>(), selectedCategory);
}
