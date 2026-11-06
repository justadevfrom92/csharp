using Microsoft.AspNetCore.Mvc;
using ProductCatalog.Data;
using ProductCatalog.Data.Entities;
using ProductCatalog.Models;

namespace ProductCatalog.Controllers;

public class ProductsController : Controller
{
    // GET /Products - display only, so the view gets the stored entities directly.
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

    // GET /Products/Create - an empty form.
    [HttpGet]
    public IActionResult Create()
    {
        var form = new ProductFormViewModel { CategoryChoices = ProductStore.Categories };
        return View(form);
    }

    // POST /Products/Create - model binding creates the ProductFormViewModel from the posted fields.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(ProductFormViewModel form)
    {
        if (!ModelState.IsValid)
        {
            form.CategoryChoices = ProductStore.Categories; // the dropdown options are not posted, so refill them
            return View(form); // show the form again with the error messages
        }

        // Copy the checked form values into a new entity and store it.
        var product = new Product
        {
            Name = form.Name,
            Category = form.Category,
            Price = form.Price,
            Description = form.Description,
        };
        ProductStore.Add(product);

        // TempData survives ONE redirect. We redirect to Index next, so ViewBag would be lost here.
        // It is READ in Views/Shared/_Layout.cshtml (the green box at the top of every page).
        TempData["SuccessMessage"] = $"Product '{product.Name}' was added.";
        return RedirectToAction(nameof(Index)); // redirect so refreshing the page doesn't post twice
    }

    // GET /Products/Edit/5 - the form, filled with the stored product's values.
    [HttpGet]
    public IActionResult Edit(int id)
    {
        Product? product = ProductStore.FindById(id);
        if (product == null)
        {
            return NotFound();
        }

        var form = new ProductFormViewModel
        {
            Id = product.Id,
            Name = product.Name,
            Category = product.Category,
            Price = product.Price,
            Description = product.Description,
            CategoryChoices = ProductStore.Categories,
        };
        return View(form);
    }

    // POST /Products/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(ProductFormViewModel form)
    {
        if (!ModelState.IsValid)
        {
            form.CategoryChoices = ProductStore.Categories;
            return View(form);
        }

        var changedProduct = new Product
        {
            Id = form.Id,
            Name = form.Name,
            Category = form.Category,
            Price = form.Price,
            Description = form.Description,
        };
        if (!ProductStore.Update(changedProduct))
        {
            return NotFound();
        }

        // Read in Views/Shared/_Layout.cshtml after the redirect (see Create above).
        TempData["SuccessMessage"] = $"Product '{form.Name}' was updated.";
        return RedirectToAction(nameof(Index));
    }
}
