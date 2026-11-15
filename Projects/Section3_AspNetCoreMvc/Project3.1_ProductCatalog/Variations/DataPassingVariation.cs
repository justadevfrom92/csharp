using Microsoft.AspNetCore.Mvc;
using ProductCatalog.Models;
using ProductCatalog.Stores;

namespace ProductCatalog.Variations;

// Four ways to give a view its data. Open /variations/data and /variations/data/set-temp.
[Route("variations/data")]
public class DataPassingVariation : Controller
{
    [HttpGet("")]
    public IActionResult Show()
    {
        ViewBag.FromViewBag = "set with ViewBag.FromViewBag";
        ViewData["FromViewData"] = "set with ViewData[\"FromViewData\"]";
        // ViewBag and ViewData are the SAME storage: setting one sets the other.
        ViewBag.Shared = "written through ViewBag, read through ViewData";
        List<ProductModel> model = ProductStore.GetAll();
        return View("~/Views/Variations/DataPassing.cshtml", model);
    }

    // TempData survives ONE redirect; ViewBag does not.
    [HttpGet("set-temp")]
    public IActionResult SetThenRedirect()
    {
        TempData["Survives"] = "TempData: still here after the redirect";
        ViewBag.Lost = "ViewBag: you will never see this";
        return RedirectToAction(nameof(Show));
    }
}
