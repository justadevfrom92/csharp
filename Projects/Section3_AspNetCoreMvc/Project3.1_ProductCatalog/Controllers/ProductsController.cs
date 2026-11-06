using Microsoft.AspNetCore.Mvc;
using ProductCatalog.Models;
using ProductCatalog.Stores;

namespace ProductCatalog.Controllers;

public class ProductsController : Controller
{
    // GET /Products
    public IActionResult Index()
    {
        return View(ProductStore.GetAll());
    }

    // Attribute routing example: GET /product/5
    [HttpGet("/product/{id:int}")]
    public IActionResult Details(int id)
    {
        ProductModel? product = ProductStore.FindById(id);
        if (product == null)
        {
            return NotFound();
        }
        return View(product);
    }

    [HttpGet]
    public IActionResult Create()
    {
        ViewBag.Categories = ProductStore.Categories;
        return View(new ProductModel());
    }

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

        // TempData survives ONE redirect. We redirect to Index next, so ViewBag would be lost here.
        // It is READ in Views/Shared/_Layout.cshtml (the green box at the top of every page).
        TempData["SuccessMessage"] = $"Product '{product.Name}' was added.";
        return RedirectToAction(nameof(Index)); // redirect so refreshing the page doesn't post twice
    }

    [HttpGet]
    public IActionResult Edit(int id)
    {
        ProductModel? product = ProductStore.FindById(id);
        if (product == null)
        {
            return NotFound();
        }
        ViewBag.Categories = ProductStore.Categories;
        return View(product);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(ProductModel product)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Categories = ProductStore.Categories;
            return View(product);
        }
        if (!ProductStore.Update(product))
        {
            return NotFound();
        }

        // Read in Views/Shared/_Layout.cshtml after the redirect (see Create above).
        TempData["SuccessMessage"] = $"Product '{product.Name}' was updated.";
        return RedirectToAction(nameof(Index));
    }
}
