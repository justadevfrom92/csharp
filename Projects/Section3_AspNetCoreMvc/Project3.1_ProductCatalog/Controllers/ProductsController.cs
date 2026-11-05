using Microsoft.AspNetCore.Mvc;
using ProductCatalog.Models;

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
        Product? product = ProductStore.FindById(id);
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
        return View(new Product());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(Product product)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Categories = ProductStore.Categories;
            return View(product); // show the form again with the error messages
        }

        ProductStore.Add(product);
        TempData["SuccessMessage"] = $"Product '{product.Name}' was added.";
        return RedirectToAction(nameof(Index)); // redirect so refreshing the page doesn't post twice
    }

    [HttpGet]
    public IActionResult Edit(int id)
    {
        Product? product = ProductStore.FindById(id);
        if (product == null)
        {
            return NotFound();
        }
        ViewBag.Categories = ProductStore.Categories;
        return View(product);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(Product product)
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

        TempData["SuccessMessage"] = $"Product '{product.Name}' was updated.";
        return RedirectToAction(nameof(Index));
    }
}
