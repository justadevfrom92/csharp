using Microsoft.AspNetCore.Mvc;
using ProductCatalog.Models;

namespace ProductCatalog.Variations;

// The same posted form, bound different ways. POST to /variations/binding/....
// (No [ValidateAntiForgeryToken] here so you can try them with curl; real forms need it - see ProductsController.)
[Route("variations/binding")]
public class ModelBindingVariation : Controller
{
    // ORIGINAL style: bind the whole model. DANGER ("over-posting"): a hacker can add Id=999 to the form
    // and it's bound too, even though the form has no Id field.
    [HttpPost("whole-model")]
    public IActionResult WholeModel(ProductModel product)
    {
        return Content($"Id={product.Id}, Name={product.Name}");
    }

    // [Bind]: ONLY the listed properties are bound. Id=999 is ignored.
    [HttpPost("bind-list")]
    public IActionResult WithBindAttribute([Bind("Name,Category,Price,Description")] ProductModel product)
    {
        return Content($"Id={product.Id}, Name={product.Name}");
    }

    // Separate parameters: nothing else CAN be bound; but no validation attributes, so check by hand.
    [HttpPost("parameters")]
    public IActionResult WithParameters(string name, decimal price)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            ModelState.AddModelError(nameof(name), "Name is required."); // manual rule
        }
        return Content($"Name={name}, Price={price}, valid={ModelState.IsValid}");
    }

    // A dedicated form class with IValidatableObject: attribute rules + a cross-field rule.
    [HttpPost("form-class")]
    public IActionResult WithFormClass(ProductFormVariation form)
    {
        string errors = string.Join(" | ", ModelState.Values.SelectMany(entry => entry.Errors).Select(error => error.ErrorMessage));
        return Content($"valid={ModelState.IsValid} {errors}");
    }
}
