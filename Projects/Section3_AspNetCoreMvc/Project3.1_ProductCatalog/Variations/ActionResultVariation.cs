using Microsoft.AspNetCore.Mvc;
using ProductCatalog.Controllers;
using ProductCatalog.Models;
using ProductCatalog.Stores;

namespace ProductCatalog.Variations;

// Return types and redirects that look alike but behave differently. Try /variations/results/....
[Route("variations/results")]
public class ActionResultVariation : Controller
{
    // ORIGINAL style: IActionResult - can return a View, NotFound, a Redirect... anything.
    [HttpGet("view/{id:int}")]
    public IActionResult AsIActionResult(int id)
    {
        ProductModel? product = ProductStore.FindById(id);
        if (product == null)
        {
            return NotFound();
        }
        return View("~/Views/Products/Details.cshtml", product); // a view from another folder: full path
    }

    // ActionResult<T>: return the OBJECT itself and MVC turns it into JSON - or return NotFound(). For APIs.
    [HttpGet("json/{id:int}")]
    public ActionResult<ProductModel> AsActionResultOfT(int id)
    {
        ProductModel? product = ProductStore.FindById(id);
        if (product == null)
        {
            return NotFound();
        }
        return product;
    }

    // RedirectToAction(nameof(...)): the compiler checks the name exists.
    [HttpGet("redirect-nameof")]
    public IActionResult RedirectWithNameof()
    {
        return RedirectToAction(nameof(ProductsController.Index), "Products");
    }

    // RedirectToAction("...") with a TYPO: compiles fine, and the general "{controller}/{action}" route happily builds
    // /Products/Indx - so the user is sent to a page that doesn't exist (404). Nothing warns you. Use nameof.
    [HttpGet("redirect-typo")]
    public IActionResult RedirectWithTypo()
    {
        return RedirectToAction("Indx", "Products");
    }

    // Redirect(url): goes ANYWHERE, including other sites ("open redirect" if the url comes from the user).
    // LocalRedirect(url): only this site; an outside url THROWS. Use it for "returnUrl"-style values (see 7.1).
    [HttpGet("redirect-any")]
    public IActionResult RedirectAnywhere(string url)
    {
        return Redirect(url);
    }

    [HttpGet("redirect-local")]
    public IActionResult RedirectLocalOnly(string url)
    {
        return LocalRedirect(url);
    }
}
